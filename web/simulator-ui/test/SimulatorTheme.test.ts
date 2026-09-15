import {
	defaultSimulatorTheme,
	getSimulatorThemeFontFamilies,
	parseSimulatorTheme,
	SIMULATOR_THEME_SCHEMA_VERSION,
	SimulatorThemeDocument,
} from '../src/model/SimulatorTheme';
import { toSimulatorFontFaceCss, toSimulatorThemeCssVariables, toTypographyStyle } from '../src/presentation/ThemeStyleHost';

const cloneDefaultTheme = (): SimulatorThemeDocument => (
	JSON.parse(JSON.stringify(defaultSimulatorTheme)) as SimulatorThemeDocument
);

describe('SimulatorTheme', () => {
	it('accepts the current default theme contract', () => {
		expect(parseSimulatorTheme(defaultSimulatorTheme)).toBe(defaultSimulatorTheme);
	});

	it('rejects unsupported schema versions', () => {
		expect(parseSimulatorTheme({
			...defaultSimulatorTheme,
			schemaVersion: SIMULATOR_THEME_SCHEMA_VERSION + 1,
		})).toBeNull();
	});

	it('rejects structurally incomplete themes', () => {
		expect(parseSimulatorTheme({
			...defaultSimulatorTheme,
			tokens: {
				global: defaultSimulatorTheme.tokens.global,
			},
		})).toBeNull();
	});

	it('rejects invalid nested token values', () => {
		expect(parseSimulatorTheme({
			...defaultSimulatorTheme,
			tokens: {
				...defaultSimulatorTheme.tokens,
				global: {
					...defaultSimulatorTheme.tokens.global,
					backgroundImage: 42,
				},
			},
		})).toBeNull();
	});

	it('maps structured tokens to presentation variables', () => {
		const variables = toSimulatorThemeCssVariables(defaultSimulatorTheme);

		expect(variables['--sim-global-background-color']).toBe('#0A0E30');
		expect(variables['--sim-question-font-size']).toBe('72px');
		expect(variables['--sim-board-gap']).toBe('4px');
		expect(variables['--sim-players-layout']).toBe('row');
		expect(variables['--sim-global-background-image']).toBe('none');
	});

	it('maps an offline Windows image path to a file URL', () => {
		const theme = {
			...defaultSimulatorTheme,
			tokens: {
				...defaultSimulatorTheme.tokens,
				global: {
					...defaultSimulatorTheme.tokens.global,
					backgroundImage: 'C:\\Broadcast\\background.png',
				},
			},
		};

		const variables = toSimulatorThemeCssVariables(theme);

		expect(variables['--sim-global-background-image'])
			.toBe('url("file:///C:/Broadcast/background.png")');
	});

	it('supports fixed font sizes, alignment, disabled shadows and plain-text boards', () => {
		const theme = cloneDefaultTheme();
		const { questionText } = theme.tokens.typography;
		questionText.autoSize = false;
		questionText.fontSize = 42;
		questionText.textAlign = 'right';
		questionText.textShadowEnabled = false;
		theme.tokens.board.bordersVisible = false;
		theme.tokens.board.plainTextOnly = true;

		expect(toTypographyStyle(questionText).fontSize).toBe('42px');
		expect(toTypographyStyle(questionText).textAlign).toBe('right');
		expect(toTypographyStyle(questionText).justifyContent).toBe('flex-end');
		expect(toTypographyStyle(questionText).textShadow).toBe('none');
		expect(toSimulatorThemeCssVariables(theme)['--sim-board-border-width']).toBe('0px');
		expect(parseSimulatorTheme(theme)).toBe(theme);
	});

	it('accepts themes saved before optional presentation switches were added', () => {
		const theme = cloneDefaultTheme();
		delete theme.tokens.typography.questionText.autoSize;
		delete theme.tokens.typography.questionText.textShadowEnabled;
		delete theme.tokens.board.bordersVisible;
		delete theme.tokens.board.plainTextOnly;

		expect(parseSimulatorTheme(theme)).toBe(theme);
	});

	it('registers an embedded font without using a system or network source', () => {
		const theme = cloneDefaultTheme();
		theme.assets.fonts['Broadcast Sans'] = 'data:font/woff2;base64,AA==';
		theme.tokens.typography.questionText.fontFamily = 'Broadcast Sans';

		expect(parseSimulatorTheme(theme)).toBe(theme);
		expect(getSimulatorThemeFontFamilies(theme)).toEqual(['Standard', 'Broadcast Sans']);
		expect(toSimulatorFontFaceCss(theme)).toContain('font-family: "Broadcast Sans"');
		expect(toSimulatorFontFaceCss(theme)).toContain('data:font/woff2;base64,AA==');
	});

	it('rejects remote font assets', () => {
		const theme = cloneDefaultTheme();
		theme.assets.fonts['Remote Sans'] = 'https://example.com/font.woff2';
		theme.tokens.typography.questionText.fontFamily = 'Remote Sans';

		expect(parseSimulatorTheme(theme)).toBeNull();
	});

	it('rejects font families that are not part of the theme registry', () => {
		const theme = cloneDefaultTheme();
		theme.tokens.typography.questionText.fontFamily = 'Arial';

		expect(parseSimulatorTheme(theme)).toBeNull();
	});
});
