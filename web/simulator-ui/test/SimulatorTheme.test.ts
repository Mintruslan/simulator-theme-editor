import {
	defaultSimulatorTheme,
	parseSimulatorTheme,
	SIMULATOR_THEME_SCHEMA_VERSION,
} from '../src/model/SimulatorTheme';
import { toSimulatorThemeCssVariables } from '../src/presentation/ThemeStyleHost';

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
});
