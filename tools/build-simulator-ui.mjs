import { createHash } from 'node:crypto';
import {
	copyFileSync,
	existsSync,
	mkdirSync,
	readdirSync,
	readFileSync,
	rmSync,
	statSync,
	writeFileSync,
} from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, '..');
const sourceDirectory = path.join(repositoryRoot, 'web', 'simulator-ui');
const buildDirectory = path.join(sourceDirectory, 'dist');
const targetDirectory = path.join(repositoryRoot, 'src', 'SImulator', 'SImulator', 'webtable');
const sigameTargetDirectory = path.join(repositoryRoot, 'src', 'SIGame', 'SIGame', 'webtable');
const integrationFile = path.join(repositoryRoot, 'web', 'simulator-ui.integration.json');
const manifestName = 'presentation-build.json';
const manifestPath = path.join(targetDirectory, manifestName);
const preservedRuntimeFiles = new Set([
	'index.html',
	'script.js',
	'style.css',
	'theme-editor.html',
	'theme-editor-script.js',
	'theme-editor.css',
]);
const ignoredBuildFiles = new Set(['index.html', 'favicon.ico']);
const shouldBuild = !process.argv.includes('--sync-only');

function fail(message) {
	console.error(`Simulator UI build failed: ${message}`);
	process.exit(1);
}

function listFiles(directory, prefix = '') {
	if (!existsSync(directory)) {
		return [];
	}

	return readdirSync(directory).flatMap((entry) => {
		const absolutePath = path.join(directory, entry);
		const relativePath = path.join(prefix, entry);

		return statSync(absolutePath).isDirectory()
			? listFiles(absolutePath, relativePath)
			: [relativePath];
	});
}

function hashFile(filePath) {
	return createHash('sha256').update(readFileSync(filePath)).digest('hex');
}

function assertSafeTarget() {
	const expectedTarget = path.join('src', 'SImulator', 'SImulator', 'webtable');
	const actualTarget = path.relative(repositoryRoot, targetDirectory);

	if (actualTarget !== expectedTarget) {
		fail(`refusing to update unexpected target: ${actualTarget}`);
	}

	const expectedSIGameTarget = path.join('src', 'SIGame', 'SIGame', 'webtable');
	const actualSIGameTarget = path.relative(repositoryRoot, sigameTargetDirectory);

	if (actualSIGameTarget !== expectedSIGameTarget) {
		fail(`refusing to update unexpected SIGame target: ${actualSIGameTarget}`);
	}
}

function syncSIGameRuntime() {
	if (existsSync(sigameTargetDirectory)) {
		rmSync(sigameTargetDirectory, { recursive: true, force: true });
	}

	for (const relativePath of listFiles(targetDirectory)) {
		const sourcePath = path.join(targetDirectory, relativePath);
		const destinationPath = path.join(sigameTargetDirectory, relativePath);
		mkdirSync(path.dirname(destinationPath), { recursive: true });
		copyFileSync(sourcePath, destinationPath);
	}
}

function runPresentationBuild() {
	const npmArguments = ['run', 'build-lib-table'];
	let npmCommand = 'npm';

	if (process.platform === 'win32') {
		const npmCliDirectories = [path.dirname(process.execPath), ...(process.env.PATH ?? '').split(path.delimiter)];

		if (process.env.ProgramFiles) {
			npmCliDirectories.push(path.join(process.env.ProgramFiles, 'nodejs'));
		}

		const npmCliCandidates = npmCliDirectories
			.filter(Boolean)
			.map((directory) => path.join(directory, 'node_modules', 'npm', 'bin', 'npm-cli.js'));
		const npmCli = npmCliCandidates.find((candidate) => existsSync(candidate));

		if (!npmCli) {
			fail('npm CLI is missing from the Node.js directory and PATH');
		}

		npmCommand = process.execPath;
		npmArguments.unshift(npmCli);
	}

	const result = spawnSync(npmCommand, npmArguments, {
		cwd: sourceDirectory,
		stdio: 'inherit',
	});

	if (result.error) {
		fail(result.error.message);
	}

	if (result.status !== 0) {
		fail(`webpack exited with code ${result.status}`);
	}
}

function removePreviousArtifacts() {
	let artifacts = [];

	if (existsSync(manifestPath)) {
		const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
		artifacts = Object.keys(manifest.artifacts ?? {});
	} else {
		// First migration from the upstream hand-maintained bundle.
		artifacts = listFiles(targetDirectory).filter((file) =>
			!preservedRuntimeFiles.has(file) && file !== manifestName);
	}

	for (const artifact of artifacts) {
		const artifactPath = path.resolve(targetDirectory, artifact);
		const relativePath = path.relative(targetDirectory, artifactPath);

		if (relativePath.startsWith('..') || path.isAbsolute(relativePath)) {
			fail(`unsafe artifact path in manifest: ${artifact}`);
		}

		if (existsSync(artifactPath)) {
			rmSync(artifactPath, { force: true });
		}
	}
}

function verifyOfflineShell() {
	for (const shellName of ['index.html', 'theme-editor.html']) {
		const html = readFileSync(path.join(targetDirectory, shellName), 'utf8');

		if (/\b(?:src|href)=["']https?:\/\//i.test(html)) {
			fail(`webtable/${shellName} contains a remote runtime dependency`);
		}
	}

	for (const file of preservedRuntimeFiles) {
		if (!existsSync(path.join(targetDirectory, file))) {
			fail(`required local runtime file is missing: ${file}`);
		}
	}
}

assertSafeTarget();

if (shouldBuild) {
	runPresentationBuild();
}

if (!existsSync(buildDirectory)) {
	fail('build output is missing; run without --sync-only first');
}

const buildFiles = listFiles(buildDirectory).filter((file) => !ignoredBuildFiles.has(file));

for (const entrypoint of ['main.js', 'vendor.js']) {
	if (!buildFiles.includes(entrypoint)) {
		fail(`required webpack entrypoint is missing: ${entrypoint}`);
	}
}

mkdirSync(targetDirectory, { recursive: true });
removePreviousArtifacts();

for (const relativePath of buildFiles) {
	const sourcePath = path.join(buildDirectory, relativePath);
	const destinationPath = path.join(targetDirectory, relativePath);
	mkdirSync(path.dirname(destinationPath), { recursive: true });
	copyFileSync(sourcePath, destinationPath);
}

verifyOfflineShell();

const integration = JSON.parse(readFileSync(integrationFile, 'utf8'));
const artifacts = Object.fromEntries(
	buildFiles
		.sort()
		.map((file) => [file.replaceAll(path.sep, '/'), hashFile(path.join(targetDirectory, file))])
);

const manifest = {
	schemaVersion: 1,
	sourceRepository: integration.sourceRepository,
	sourceRevision: integration.sourceRevision,
	packageLockSha256: hashFile(path.join(sourceDirectory, 'package-lock.json')),
	entrypoints: ['vendor.js', 'main.js', 'script.js', 'theme-editor-script.js'],
	offlineRuntime: true,
	artifacts,
};

writeFileSync(manifestPath, `${JSON.stringify(manifest, null, 2)}\n`);
syncSIGameRuntime();
console.log(`SI presentation synced: ${buildFiles.length} files -> ${path.relative(repositoryRoot, targetDirectory)}, ${path.relative(repositoryRoot, sigameTargetDirectory)}`);
