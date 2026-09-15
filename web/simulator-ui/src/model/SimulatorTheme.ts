export const SIMULATOR_THEME_SCHEMA_VERSION = 1;
export const SIMULATOR_BUILT_IN_FONT_FAMILY = 'Standard';
export const MAX_SIMULATOR_THEME_FONTS = 8;
export const MAX_SIMULATOR_FONT_FILE_SIZE = 5 * 1024 * 1024;

const MAX_SIMULATOR_FONT_DATA_URL_LENGTH = (Math.ceil(MAX_SIMULATOR_FONT_FILE_SIZE / 3) * 4) + 64;
const supportedFontDataUrlPrefixes = [
	'data:font/ttf;base64,',
	'data:font/otf;base64,',
	'data:font/woff;base64,',
	'data:font/woff2;base64,',
	'data:application/x-font-ttf;base64,',
	'data:application/x-font-opentype;base64,',
	'data:application/font-woff;base64,',
	'data:application/font-woff2;base64,',
	'data:application/octet-stream;base64,',
];

export type SimulatorTextAlignment = 'left' | 'center' | 'right';
export type SimulatorPlayerLayout = 'horizontal' | 'vertical';
export type SimulatorBackgroundFit = 'cover' | 'contain' | 'stretch' | 'center';

export interface SimulatorTypographyToken {
	fontFamily: string;
	fontSize: number;
	fontWeight: number;
	lineHeight: string;
	letterSpacing: number;
	color: string;
	textAlign: SimulatorTextAlignment;
	textTransform: 'none' | 'uppercase' | 'lowercase';
	textShadow: string;
}

export interface SimulatorThemeDocument {
	schemaVersion: number;
	id: string;
	name: string;
	basedOn: string | null;
	assets: {
		fonts: Record<string, string>;
		images: Record<string, string>;
	};
	tokens: {
		global: {
			backgroundColor: string;
			backgroundImage: string | null;
			backgroundFit: SimulatorBackgroundFit;
			backgroundPosition: string;
			textColor: string;
			accentColor: string;
			borderRadius: number;
			scale: number;
		};
		typography: {
			themeName: SimulatorTypographyToken;
			finalThemeName: SimulatorTypographyToken;
			questionPrice: SimulatorTypographyToken;
			questionText: SimulatorTypographyToken;
			answerText: SimulatorTypographyToken;
			playerName: SimulatorTypographyToken;
			playerScore: SimulatorTypographyToken;
			timer: SimulatorTypographyToken;
		};
		board: {
			gap: number;
			padding: number;
			cellBackground: string;
			themeHeaderBackground: string;
			borderWidth: number;
			borderColor: string;
			borderRadius: number;
			hoverBackground: string;
			selectedBackground: string;
			answeredOpacity: number;
		};
		question: {
			background: string;
			contentWidth: string;
			padding: number;
			alignment: SimulatorTextAlignment;
			imageFit: 'cover' | 'contain' | 'fill';
		};
		players: {
			layout: SimulatorPlayerLayout;
			gap: number;
			cardBackground: string;
			cardBorderColor: string;
			cardBorderWidth: number;
			cardBorderRadius: number;
			buzzerBackground: string;
			correctBackground: string;
			incorrectBackground: string;
		};
		effects: {
			shadow: string;
			transitionDuration: number;
			motionScale: number;
		};
	};
}

const typography = (
	fontSize: number,
	fontWeight: number,
	color = '#FFFFFF',
): SimulatorTypographyToken => ({
	fontFamily: SIMULATOR_BUILT_IN_FONT_FAMILY,
	fontSize,
	fontWeight,
	lineHeight: 'normal',
	letterSpacing: 0,
	color,
	textAlign: 'center',
	textTransform: 'none',
	textShadow: '2px 2px 4px rgba(0, 0, 0, 0.7)',
});

/** Visual baseline used whenever no saved SImulator theme is available. */
export const defaultSimulatorTheme: SimulatorThemeDocument = {
	schemaVersion: SIMULATOR_THEME_SCHEMA_VERSION,
	id: 'simulator-default',
	name: 'Default SImulator Theme',
	basedOn: null,
	assets: {
		fonts: {},
		images: {},
	},
	tokens: {
		global: {
			backgroundColor: '#0A0E30',
			backgroundImage: null,
			backgroundFit: 'cover',
			backgroundPosition: 'center',
			textColor: '#FFFFFF',
			accentColor: '#FF8C00',
			borderRadius: 4,
			scale: 1,
		},
		typography: {
			themeName: typography(60, 500),
			finalThemeName: typography(144, 500),
			questionPrice: typography(144, 500),
			questionText: typography(72, 500),
			answerText: typography(72, 500),
			playerName: typography(48, 700),
			playerScore: typography(48, 700),
			timer: typography(16, 500),
		},
		board: {
			gap: 4,
			padding: 2,
			cellBackground: 'transparent',
			themeHeaderBackground: 'rgba(255, 255, 255, 0.05)',
			borderWidth: 2,
			borderColor: 'transparent',
			borderRadius: 4,
			hoverBackground: 'rgba(255, 255, 255, 0.3)',
			selectedBackground: 'rgba(255, 255, 255, 0.15)',
			answeredOpacity: 0,
		},
		question: {
			background: 'transparent',
			contentWidth: '100%',
			padding: 5,
			alignment: 'center',
			imageFit: 'contain',
		},
		players: {
			layout: 'horizontal',
			gap: 0,
			cardBackground: 'linear-gradient(135deg, rgba(20, 24, 62, 0.4), rgba(5, 6, 31, 0.6))',
			cardBorderColor: 'rgba(255, 255, 255, 0.15)',
			cardBorderWidth: 1,
			cardBorderRadius: 10,
			buzzerBackground: 'linear-gradient(135deg, rgba(236, 232, 164, 0.75), rgba(182, 170, 67, 0.95))',
			correctBackground: 'linear-gradient(135deg, rgba(76, 175, 80, 0.75), rgba(46, 125, 50, 0.95))',
			incorrectBackground: 'linear-gradient(135deg, rgba(244, 67, 54, 0.75), rgba(198, 40, 40, 0.95))',
		},
		effects: {
			shadow: '0 4px 12px rgba(0, 0, 0, 0.3)',
			transitionDuration: 300,
			motionScale: 1,
		},
	},
};

const isObject = (value: unknown): value is Record<string, unknown> => (
	typeof value === 'object' && value !== null && !Array.isArray(value)
);

const isString = (value: unknown): value is string => typeof value === 'string';
const isNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value);

function hasStringValues(value: unknown): boolean {
	return isObject(value) && Object.values(value).every(isString);
}

function isValidFontFamilyName(value: string): boolean {
	return value.length > 0 && value.length <= 64 && Array.from(value).every(character => {
		const characterCode = character.charCodeAt(0);
		return characterCode >= 32 && characterCode !== 127 && !'{};"\\'.includes(character);
	});
}

/** Only embedded font payloads are accepted so a theme never depends on machine or network fonts. */
export function isSimulatorFontDataUrl(value: string): boolean {
	if (value.length > MAX_SIMULATOR_FONT_DATA_URL_LENGTH) {
		return false;
	}

	const normalizedValue = value.toLowerCase();
	return supportedFontDataUrlPrefixes.some(prefix => (
		normalizedValue.startsWith(prefix) && value.length > prefix.length
	));
}

function hasFontAssets(value: unknown): value is Record<string, string> {
	if (!isObject(value)) {
		return false;
	}

	const entries = Object.entries(value);

	return entries.length <= MAX_SIMULATOR_THEME_FONTS && entries.every(([fontFamily, source]) => (
		isString(source) &&
		fontFamily.toLowerCase() !== SIMULATOR_BUILT_IN_FONT_FAMILY.toLowerCase() &&
		isValidFontFamilyName(fontFamily) &&
		isSimulatorFontDataUrl(source)
	));
}

/** Lists the complete, portable font registry available to Theme Editor controls. */
export function getSimulatorThemeFontFamilies(theme: SimulatorThemeDocument): string[] {
	return [SIMULATOR_BUILT_IN_FONT_FAMILY, ...Object.keys(theme.assets.fonts).sort((left, right) => left.localeCompare(right))];
}

function isTypographyToken(value: unknown, fontFamilies: Set<string>): boolean {
	return isObject(value) &&
		isString(value.fontFamily) &&
		fontFamilies.has(value.fontFamily) &&
		isNumber(value.fontSize) &&
		isNumber(value.fontWeight) &&
		isString(value.lineHeight) &&
		isNumber(value.letterSpacing) &&
		isString(value.color) &&
		['left', 'center', 'right'].includes(value.textAlign as string) &&
		['none', 'uppercase', 'lowercase'].includes(value.textTransform as string) &&
		isString(value.textShadow);
}

function hasTypographyTokens(value: Record<string, unknown>, fontFamilies: Set<string>): boolean {
	const roles = [
		'themeName',
		'finalThemeName',
		'questionPrice',
		'questionText',
		'answerText',
		'playerName',
		'playerScore',
		'timer',
	];

	return roles.every((role) => isTypographyToken(value[role], fontFamilies));
}

function hasGlobalTokens(value: Record<string, unknown>): boolean {
	return isString(value.backgroundColor) &&
		(value.backgroundImage === null || isString(value.backgroundImage)) &&
		['cover', 'contain', 'stretch', 'center'].includes(value.backgroundFit as string) &&
		isString(value.backgroundPosition) &&
		isString(value.textColor) &&
		isString(value.accentColor) &&
		isNumber(value.borderRadius) &&
		isNumber(value.scale);
}

function hasBoardTokens(value: Record<string, unknown>): boolean {
	return isNumber(value.gap) &&
		isNumber(value.padding) &&
		isString(value.cellBackground) &&
		isString(value.themeHeaderBackground) &&
		isNumber(value.borderWidth) &&
		isString(value.borderColor) &&
		isNumber(value.borderRadius) &&
		isString(value.hoverBackground) &&
		isString(value.selectedBackground) &&
		isNumber(value.answeredOpacity);
}

function hasQuestionTokens(value: Record<string, unknown>): boolean {
	return isString(value.background) &&
		isString(value.contentWidth) &&
		isNumber(value.padding) &&
		['left', 'center', 'right'].includes(value.alignment as string) &&
		['cover', 'contain', 'fill'].includes(value.imageFit as string);
}

function hasPlayerTokens(value: Record<string, unknown>): boolean {
	return ['horizontal', 'vertical'].includes(value.layout as string) &&
		isNumber(value.gap) &&
		isString(value.cardBackground) &&
		isString(value.cardBorderColor) &&
		isNumber(value.cardBorderWidth) &&
		isNumber(value.cardBorderRadius) &&
		isString(value.buzzerBackground) &&
		isString(value.correctBackground) &&
		isString(value.incorrectBackground);
}

function hasEffectTokens(value: Record<string, unknown>): boolean {
	return isString(value.shadow) &&
		isNumber(value.transitionDuration) &&
		isNumber(value.motionScale);
}

/** Rejects unsupported or structurally incomplete theme messages at the WebView boundary. */
export function parseSimulatorTheme(value: unknown): SimulatorThemeDocument | null {
	if (!isObject(value) ||
		value.schemaVersion !== SIMULATOR_THEME_SCHEMA_VERSION ||
		typeof value.id !== 'string' ||
		typeof value.name !== 'string' ||
		!(value.basedOn === null || isString(value.basedOn)) ||
		!isObject(value.assets) ||
		!hasFontAssets(value.assets.fonts) ||
		!hasStringValues(value.assets.images)) {
		return null;
	}

	const { tokens } = value;
	const fontFamilies = new Set([
		SIMULATOR_BUILT_IN_FONT_FAMILY,
		...Object.keys(value.assets.fonts as Record<string, string>),
	]);

	if (!isObject(tokens)) {
		return null;
	}

	if (!isObject(tokens.global) || !hasGlobalTokens(tokens.global) ||
		!isObject(tokens.typography) || !hasTypographyTokens(tokens.typography, fontFamilies) ||
		!isObject(tokens.board) || !hasBoardTokens(tokens.board) ||
		!isObject(tokens.question) || !hasQuestionTokens(tokens.question) ||
		!isObject(tokens.players) || !hasPlayerTokens(tokens.players) ||
		!isObject(tokens.effects) || !hasEffectTokens(tokens.effects)) {
		return null;
	}

	return value as unknown as SimulatorThemeDocument;
}
