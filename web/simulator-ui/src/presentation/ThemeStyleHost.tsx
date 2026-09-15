import React from 'react';
import { useAppSelector } from '../state/hooks';
import { SimulatorThemeDocument, SimulatorTypographyToken } from '../model/SimulatorTheme';

import './ThemeStyleHost.scss';

type ThemeCssVariables = React.CSSProperties & Record<`--sim-${string}`, string | number>;

interface ThemeStyleHostProps {
	children: React.ReactNode;
	className?: string;
}

const pixels = (value: number): string => `${value}px`;
const milliseconds = (value: number): string => `${value}ms`;

/** Maps one semantic typography role to the inline style consumed by AutoSizedText. */
export function toTypographyStyle(token: SimulatorTypographyToken): React.CSSProperties {
	return {
		fontFamily: token.fontFamily,
		fontWeight: token.fontWeight,
		lineHeight: token.lineHeight,
		letterSpacing: pixels(token.letterSpacing),
		color: token.color,
		textAlign: token.textAlign,
		textTransform: token.textTransform,
		textShadow: token.textShadow,
	};
}

function cssUrl(value: string | null): string {
	if (!value) {
		return 'none';
	}

	return `url("${value.replaceAll('\\', '\\\\').replaceAll('"', '\\"')}")`;
}

function toBackgroundSize(backgroundFit: string): string {
	switch (backgroundFit) {
		case 'stretch':
			return '100% 100%';

		case 'center':
			return 'auto';

		default:
			return backgroundFit;
	}
}

/** Converts structured theme data into implementation-level CSS custom properties. */
export function toSimulatorThemeCssVariables(theme: SimulatorThemeDocument): ThemeCssVariables {
	const { global, typography, board, question, players, effects } = theme.tokens;
	const backgroundSize = toBackgroundSize(global.backgroundFit);

	return {
		'--sim-global-background-color': global.backgroundColor,
		'--sim-global-background-image': cssUrl(global.backgroundImage),
		'--sim-global-background-fit': backgroundSize,
		'--sim-global-background-position': global.backgroundPosition,
		'--sim-global-text-color': global.textColor,
		'--sim-global-accent-color': global.accentColor,
		'--sim-global-radius': pixels(global.borderRadius),
		'--sim-global-scale': global.scale,
		'--sim-theme-name-font-family': typography.themeName.fontFamily,
		'--sim-theme-name-font-size': pixels(typography.themeName.fontSize),
		'--sim-theme-name-font-weight': typography.themeName.fontWeight,
		'--sim-theme-name-color': typography.themeName.color,
		'--sim-final-theme-name-font-family': typography.finalThemeName.fontFamily,
		'--sim-final-theme-name-font-size': pixels(typography.finalThemeName.fontSize),
		'--sim-final-theme-name-font-weight': typography.finalThemeName.fontWeight,
		'--sim-final-theme-name-color': typography.finalThemeName.color,
		'--sim-question-price-font-family': typography.questionPrice.fontFamily,
		'--sim-question-price-font-size': pixels(typography.questionPrice.fontSize),
		'--sim-question-price-font-weight': typography.questionPrice.fontWeight,
		'--sim-question-price-color': typography.questionPrice.color,
		'--sim-question-font-family': typography.questionText.fontFamily,
		'--sim-question-font-size': pixels(typography.questionText.fontSize),
		'--sim-question-font-weight': typography.questionText.fontWeight,
		'--sim-question-line-height': typography.questionText.lineHeight,
		'--sim-question-letter-spacing': pixels(typography.questionText.letterSpacing),
		'--sim-question-color': typography.questionText.color,
		'--sim-question-text-shadow': typography.questionText.textShadow,
		'--sim-answer-font-family': typography.answerText.fontFamily,
		'--sim-answer-font-size': pixels(typography.answerText.fontSize),
		'--sim-answer-color': typography.answerText.color,
		'--sim-player-name-font-family': typography.playerName.fontFamily,
		'--sim-player-name-font-size': pixels(typography.playerName.fontSize),
		'--sim-player-name-color': typography.playerName.color,
		'--sim-player-score-font-family': typography.playerScore.fontFamily,
		'--sim-player-score-font-size': pixels(typography.playerScore.fontSize),
		'--sim-player-score-color': typography.playerScore.color,
		'--sim-timer-color': typography.timer.color,
		'--sim-board-gap': pixels(board.gap),
		'--sim-board-padding': pixels(board.padding),
		'--sim-board-cell-background': board.cellBackground,
		'--sim-board-theme-background': board.themeHeaderBackground,
		'--sim-board-border-width': pixels(board.borderWidth),
		'--sim-board-border-color': board.borderColor,
		'--sim-board-radius': pixels(board.borderRadius),
		'--sim-board-hover-background': board.hoverBackground,
		'--sim-board-selected-background': board.selectedBackground,
		'--sim-board-answered-opacity': board.answeredOpacity,
		'--sim-question-background': question.background,
		'--sim-question-content-width': question.contentWidth,
		'--sim-question-padding': pixels(question.padding),
		'--sim-question-alignment': question.alignment,
		'--sim-question-image-fit': question.imageFit,
		'--sim-players-layout': players.layout === 'horizontal' ? 'row' : 'column',
		'--sim-players-gap': pixels(players.gap),
		'--sim-player-card-background': players.cardBackground,
		'--sim-player-card-border-color': players.cardBorderColor,
		'--sim-player-card-border-width': pixels(players.cardBorderWidth),
		'--sim-player-card-radius': pixels(players.cardBorderRadius),
		'--sim-player-buzzer-background': players.buzzerBackground,
		'--sim-player-correct-background': players.correctBackground,
		'--sim-player-incorrect-background': players.incorrectBackground,
		'--sim-effect-shadow': effects.shadow,
		'--sim-transition-duration': milliseconds(effects.transitionDuration),
		'--sim-motion-scale': effects.motionScale,
	};
}

/** Owns the theme boundary shared by live presentation and future preview surfaces. */
export default function ThemeStyleHost({ children, className = '' }: ThemeStyleHostProps): JSX.Element {
	const theme = useAppSelector((state) => state.settings.simulatorTheme);
	const cssVariables = toSimulatorThemeCssVariables(theme);

	return (
		<div className={`simulatorPresentation ${className}`} style={cssVariables}>
			{children}
		</div>
	);
}
