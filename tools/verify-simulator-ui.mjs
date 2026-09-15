import { createServer } from 'node:http';
import { createRequire } from 'node:module';
import { readFileSync, statSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const repositoryRoot = path.resolve(scriptDirectory, '..');
const webtableDirectory = path.join(repositoryRoot, 'src', 'SImulator', 'SImulator', 'webtable');
const requireFromPresentation = createRequire(path.join(repositoryRoot, 'web', 'simulator-ui', 'package.json'));
const { chromium } = requireFromPresentation('@playwright/test');
const mimeTypes = {
	'.css': 'text/css',
	'.eot': 'application/vnd.ms-fontobject',
	'.html': 'text/html',
	'.jpg': 'image/jpeg',
	'.js': 'text/javascript',
	'.json': 'application/json',
	'.mp3': 'audio/mpeg',
	'.png': 'image/png',
	'.svg': 'image/svg+xml',
	'.ttf': 'font/ttf',
	'.woff': 'font/woff',
	'.woff2': 'font/woff2',
};

function resolveRequestPath(requestUrl) {
	const pathname = decodeURIComponent(new URL(requestUrl, 'http://127.0.0.1').pathname);
	const requestedFile = pathname === '/' ? 'index.html' : pathname.slice(1);
	const absolutePath = path.resolve(webtableDirectory, requestedFile);
	const relativePath = path.relative(webtableDirectory, absolutePath);

	if (relativePath.startsWith('..') || path.isAbsolute(relativePath)) {
		return null;
	}

	return absolutePath;
}

const server = createServer((request, response) => {
	const filePath = resolveRequestPath(request.url ?? '/');

	try {
		if (!filePath || !statSync(filePath).isFile()) {
			response.writeHead(404);
			response.end();
			return;
		}

		response.writeHead(200, {
			'Content-Type': mimeTypes[path.extname(filePath)] ?? 'application/octet-stream',
		});
		response.end(readFileSync(filePath));
	} catch {
		response.writeHead(404);
		response.end();
	}
});

await new Promise((resolve) => server.listen(0, '127.0.0.1', resolve));
const address = server.address();

if (!address || typeof address === 'string') {
	server.close();
	throw new Error('Could not start the local presentation server');
}

const localOrigin = `http://127.0.0.1:${address.port}`;
const browser = await chromium.launch({ headless: true });

try {
	const context = await browser.newContext({ viewport: { width: 1280, height: 720 } });
	const page = await context.newPage();
	const pageErrors = [];
	const externalRequests = [];

	page.on('pageerror', (error) => pageErrors.push(error.message));
	page.on('request', (request) => {
		if (!request.url().startsWith(localOrigin)) {
			externalRequests.push(request.url());
		}
	});

	await page.addInitScript(() => {
		class MockWebView extends EventTarget {
			constructor() {
				super();
				this.postedMessages = [];
			}

			postMessage(message) {
				this.postedMessages.push(message);
			}

			receive(message) {
				this.dispatchEvent(new MessageEvent('message', { data: message }));
			}
		}

		const chromeHost = window.chrome ?? {};

		Object.defineProperty(chromeHost, 'webview', {
			configurable: true,
			value: new MockWebView(),
		});
		Object.defineProperty(window, 'chrome', {
			configurable: true,
			value: chromeHost,
		});
	});

	await page.goto(localOrigin, { waitUntil: 'networkidle' });
	await page.waitForFunction(() => window.chrome.webview.postedMessages.some((message) => message.type === 'loaded'));

	await page.evaluate(() => {
		const theme = structuredClone(window.sigame.defaultSimulatorTheme);
		theme.id = 'smoke-theme';
		theme.name = 'Smoke Theme';
		theme.tokens.global.backgroundColor = '#102040';
		theme.tokens.board.gap = 8;

		const messages = [
			{ type: 'applyTheme', theme },
			{
				type: 'roundThemes',
				themes: ['Science', 'Games', 'Culture'],
				playMode: 'None',
			},
			{
				type: 'table',
				table: Array.from({ length: 3 }, () => [100, 200, 300, 400, 500]),
			},
			{ type: 'showTable' },
			{ type: 'choose' },
		];

		messages.forEach((message) => window.chrome.webview.receive(message));
	});

	await page.waitForSelector('.roundTable .themeHeader');

	const result = await page.evaluate(() => {
		const presentation = document.querySelector('.simulatorPresentation');
		const table = document.querySelector('#table');

		return {
			background: presentation ? getComputedStyle(presentation).backgroundColor : null,
			boardGap: presentation ? getComputedStyle(presentation).getPropertyValue('--sim-board-gap').trim() : null,
			cellCount: document.querySelectorAll('.roundTableCell').length,
			tableBackground: table ? getComputedStyle(table).backgroundColor : null,
		};
	});

	if (pageErrors.length > 0) {
		throw new Error(`Presentation emitted page errors: ${pageErrors.join('; ')}`);
	}

	if (externalRequests.length > 0) {
		throw new Error(`Presentation requested external resources: ${externalRequests.join(', ')}`);
	}

	if (result.background !== 'rgb(16, 32, 64)' ||
		result.boardGap !== '8px' ||
		result.cellCount !== 18 ||
		result.tableBackground !== 'rgba(0, 0, 0, 0)') {
		throw new Error(`Unexpected presentation state: ${JSON.stringify(result)}`);
	}

	console.log(`Simulator UI smoke test passed: ${JSON.stringify(result)}`);

	await page.goto(`${localOrigin}/theme-editor.html`, { waitUntil: 'networkidle' });
	await page.waitForSelector('.themeEditor .themeEditorPreviewStage');

	await page.locator('.themeEditorInspector input[type="color"]').first().fill('#223344');

	await page.waitForFunction(() => {
		const preview = document.querySelector('.themeEditorPreviewStage');
		return preview && getComputedStyle(preview).backgroundColor === 'rgb(34, 51, 68)';
	});

	const previewChecks = [
		['Текст', '#table .tableText'],
		['Изображение', '#table .inGameImg'],
		['Видео', '.themeEditorMediaPlaceholder.video'],
		['Аудио', '.themeEditorMediaPlaceholder.audio'],
		['Игроки', '.playersPanel .gamePlayer'],
		['Кнопка', '.playersPanel .state_press'],
		['Верно', '.playersPanel .state_right'],
		['Неверно', '.playersPanel .state_wrong'],
		['Финал', '#table .finalTable'],
		['Табло', '#table .roundTable'],
	];

	for (const [buttonName, expectedSelector] of previewChecks) {
		await page.locator('.themeEditorPreviewStates').getByRole('button', { name: buttonName, exact: true }).click();
		await page.waitForSelector(expectedSelector);
	}

	await page.locator('.themeEditorNavigation').getByRole('button', { name: 'Типографика', exact: true }).click();
	await page.locator('.themeEditorFontInput').setInputFiles(
		path.join(repositoryRoot, 'web', 'simulator-ui', 'assets', 'fonts', 'Jost-Regular.ttf'),
	);
	await page.waitForFunction(() => document.querySelectorAll('.themeEditorFontFamily option').length === 2);
	await page.locator('.themeEditorPreviewStates').getByRole('button', { name: 'Текст', exact: true }).click();
	await page.waitForSelector('#table .tableText');
	await page.waitForFunction(() => document.fonts.check('16px "Jost-Regular"'));

	const editorResult = await page.evaluate(() => ({
		background: getComputedStyle(document.querySelector('.themeEditorPreviewStage')).backgroundColor,
		previewStateCount: document.querySelectorAll('.themeEditorPreviewStates button').length,
		sectionCount: document.querySelectorAll('.themeEditorNavigation button').length,
		fontOptionCount: document.querySelectorAll('.themeEditorFontFamily option').length,
		selectedFont: document.querySelector('.themeEditorFontFamily').value,
		fontFaceIsEmbedded: document.querySelector('style[data-simulator-theme-fonts]')?.textContent
			.includes('data:font/ttf;base64,') ?? false,
		postedMessageTypes: window.chrome.webview.postedMessages.map((message) => message.type),
	}));

	if (editorResult.background !== 'rgb(34, 51, 68)' ||
		editorResult.previewStateCount !== 10 ||
		editorResult.sectionCount !== 5 ||
		editorResult.fontOptionCount !== 2 ||
		editorResult.selectedFont !== 'Jost-Regular' ||
		!editorResult.fontFaceIsEmbedded ||
		!editorResult.postedMessageTypes.includes('loaded') ||
		!editorResult.postedMessageTypes.includes('themeChanged')) {
		throw new Error(`Unexpected Theme Editor state: ${JSON.stringify(editorResult)}`);
	}

	if (pageErrors.length > 0) {
		throw new Error(`Presentation emitted page errors: ${pageErrors.join('; ')}`);
	}

	if (externalRequests.length > 0) {
		throw new Error(`Presentation requested external resources: ${externalRequests.join(', ')}`);
	}

	console.log(`Theme Editor smoke test passed: ${JSON.stringify(editorResult)}`);

	if (process.argv.includes('--screenshot')) {
		const screenshotPath = path.join('/private/tmp', 'simulator-theme-editor.png');
		await page.screenshot({ path: screenshotPath, fullPage: true });
		console.log(`Theme Editor screenshot saved: ${screenshotPath}`);
	}

	await context.close();
} finally {
	await browser.close();
	server.close();
}
