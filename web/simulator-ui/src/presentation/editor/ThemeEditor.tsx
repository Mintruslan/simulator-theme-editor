import React from 'react';
import GameTable from '../../components/gameTable/GameTable/GameTable';
import PlayersView from '../../components/game/PlayersView/PlayersView';
import { AudioContextProvider } from '../../contexts/AudioContextProvider';
import ContentType from '../../model/enums/ContentType';
import PlayerStates from '../../model/enums/PlayerStates';
import PlayerInfo from '../../model/PlayerInfo';
import {
	defaultSimulatorTheme,
	getSimulatorThemeFontFamilies,
	MAX_SIMULATOR_FONT_FILE_SIZE,
	MAX_SIMULATOR_THEME_FONTS,
	parseSimulatorTheme,
	SIMULATOR_BUILT_IN_FONT_FAMILY,
	SimulatorThemeDocument,
	SimulatorTypographyToken,
} from '../../model/SimulatorTheme';
import { useAppDispatch, useAppSelector } from '../../state/hooks';
import { infoChanged, playerStateChanged } from '../../state/room2Slice';
import { applySimulatorTheme } from '../../state/settingsSlice';
import {
	setRoundThemes,
	showContent,
	showRoundTable,
	showText,
	showThemeStack,
} from '../../state/tableSlice';
import { playersVisibilityChanged } from '../../state/uiSlice';
import ThemeStyleHost from '../ThemeStyleHost';
import previewImage from '../../../assets/images/logo_table.png';

import './ThemeEditor.scss';

type EditorSection = 'global' | 'typography' | 'board' | 'question' | 'players';
type TypographyRole = keyof SimulatorThemeDocument['tokens']['typography'];
type PreviewState = 'board' | 'question' | 'image' | 'video' | 'audio' | 'players' |
	'buzzer' | 'correct' | 'incorrect' | 'final';

interface ThemeLibraryItem {
	id: string;
	name: string;
	basedOn: string | null;
}

interface EditorMessage {
	type: string;
	theme?: unknown;
	themes?: ThemeLibraryItem[];
	activeThemeId?: string;
	message?: string;
	level?: 'success' | 'error';
}

const editorSections: Array<{ id: EditorSection; label: string }> = [
	{ id: 'global', label: 'Основное' },
	{ id: 'typography', label: 'Типографика' },
	{ id: 'board', label: 'Табло' },
	{ id: 'question', label: 'Вопрос' },
	{ id: 'players', label: 'Игроки' },
];

const previewStates: Array<{ id: PreviewState; label: string }> = [
	{ id: 'board', label: 'Табло' },
	{ id: 'question', label: 'Текст' },
	{ id: 'image', label: 'Изображение' },
	{ id: 'video', label: 'Видео' },
	{ id: 'audio', label: 'Аудио' },
	{ id: 'players', label: 'Игроки' },
	{ id: 'buzzer', label: 'Кнопка' },
	{ id: 'correct', label: 'Верно' },
	{ id: 'incorrect', label: 'Неверно' },
	{ id: 'final', label: 'Финал' },
];

const typographyRoles: Array<{ id: TypographyRole; label: string }> = [
	{ id: 'themeName', label: 'Название темы' },
	{ id: 'finalThemeName', label: 'Тема финала' },
	{ id: 'questionPrice', label: 'Стоимость вопроса' },
	{ id: 'questionText', label: 'Текст вопроса' },
	{ id: 'answerText', label: 'Текст ответа' },
	{ id: 'playerName', label: 'Имя игрока' },
	{ id: 'playerScore', label: 'Счёт игрока' },
	{ id: 'timer', label: 'Таймер' },
];

const mockThemes = [
	{ name: 'НАУКА', comment: '', questions: [100, 200, 300, 400, 500] },
	{ name: 'ВИДЕОИГРЫ', comment: '', questions: [100, 200, -1, 400, 500] },
	{ name: 'КИНО И СЕРИАЛЫ', comment: '', questions: [100, 200, 300, 400, 500] },
	{ name: 'ТЕХНОЛОГИИ', comment: '', questions: [100, 200, 300, 400, 500] },
	{ name: 'МУЗЫКА', comment: '', questions: [100, 200, 300, 400, 500] },
];

const finalThemes = ['Игры', 'Космос', 'Литература', 'Музыка', 'Технологии']
	.map(name => ({ name, comment: '', questions: [] }));

function createPlayer(name: string, sum: number): PlayerInfo {
	return {
		name,
		sum,
		stake: 0,
		state: PlayerStates.None,
		canBeSelected: false,
		isReady: true,
		replic: null,
		isDeciding: false,
		isHuman: true,
		isChooser: false,
		inGame: true,
		mediaLoaded: true,
		mediaPreloaded: true,
		mediaPreloadProgress: 1,
		answer: '',
		isAppellating: false,
	};
}

const mockPlayers = [
	createPlayer('АЛЕКСЕЙ', 3200),
	createPlayer('МАРИЯ', 4700),
	createPlayer('ДМИТРИЙ', 2900),
];

const cloneTheme = (theme: SimulatorThemeDocument): SimulatorThemeDocument => (
	JSON.parse(JSON.stringify(theme)) as SimulatorThemeDocument
);

function postEditorMessage(message: object): boolean {
	const webview = window.chrome?.webview;

	if (!webview) {
		return false;
	}

	webview.postMessage(message);
	return true;
}

function toColorInput(value: string): string {
	return /^#[0-9a-f]{6}$/i.test(value) ? value : '#000000';
}

const supportedFontMimeTypes: Record<string, string> = {
	ttf: 'font/ttf',
	otf: 'font/otf',
	woff: 'font/woff',
	woff2: 'font/woff2',
};

function createFontFamily(fileName: string, fonts: Record<string, string>): string {
	const baseName = fileName
		.replace(/\.[^.]+$/, '')
		.replace(/[^A-Za-z0-9А-Яа-яЁё _.-]/g, ' ')
		.replace(/\s+/g, ' ')
		.trim()
		.slice(0, 54) || 'Custom Font';
	const existingNames = new Set(Object.keys(fonts).map(name => name.toLowerCase()));
	let fontFamily = baseName;
	let suffix = 2;

	while (existingNames.has(fontFamily.toLowerCase()) ||
		fontFamily.toLowerCase() === SIMULATOR_BUILT_IN_FONT_FAMILY.toLowerCase()) {
		fontFamily = `${baseName} ${suffix}`;
		suffix += 1;
	}

	return fontFamily;
}

function readFontAsDataUrl(file: File, mimeType: string): Promise<string> {
	return new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.onerror = () => reject(reader.error ?? new Error('Не удалось прочитать файл шрифта'));
		reader.onload = () => {
			const result = typeof reader.result === 'string' ? reader.result : '';
			const separatorIndex = result.indexOf(',');

			if (separatorIndex === -1) {
				reject(new Error('Не удалось подготовить файл шрифта'));
				return;
			}

			resolve(`data:${mimeType};base64,${result.slice(separatorIndex + 1)}`);
		};
		reader.readAsDataURL(file);
	});
}

interface FieldProps {
	label: string;
	children: React.ReactNode;
}

function Field({ label, children }: FieldProps): JSX.Element {
	return (
		<label className='themeEditorField'>
			<span>{label}</span>
			{children}
		</label>
	);
}

export default function ThemeEditor(): JSX.Element {
	const dispatch = useAppDispatch();
	const theme = useAppSelector(state => state.settings.simulatorTheme);
	const [section, setSection] = React.useState<EditorSection>('global');
	const [typographyRole, setTypographyRole] = React.useState<TypographyRole>('questionText');
	const [previewState, setPreviewState] = React.useState<PreviewState>('board');
	const [library, setLibrary] = React.useState<ThemeLibraryItem[]>([
		{ id: defaultSimulatorTheme.id, name: defaultSimulatorTheme.name, basedOn: null },
	]);
	const [activeThemeId, setActiveThemeId] = React.useState(theme.id);
	const [status, setStatus] = React.useState('Все изменения сразу отображаются в preview');
	const importInput = React.useRef<HTMLInputElement>(null);
	const fontImportInput = React.useRef<HTMLInputElement>(null);

	React.useEffect(() => {
		dispatch(infoChanged({
			all: {},
			showman: {
				name: 'ВЕДУЩИЙ',
				isReady: true,
				replic: null,
				isDeciding: false,
				isHuman: true,
			},
			players: mockPlayers,
		}));
	}, [dispatch]);

	React.useEffect(() => {
		dispatch(playersVisibilityChanged([
			'players', 'buzzer', 'correct', 'incorrect',
		].includes(previewState)));

		mockPlayers.forEach((_, index) => dispatch(playerStateChanged({ index, state: PlayerStates.None })));

		switch (previewState) {
			case 'board':
				dispatch(setRoundThemes(mockThemes));
				dispatch(showRoundTable());
				break;

			case 'image':
				dispatch(showContent([{
					content: [{ type: ContentType.Image, value: previewImage, read: false, partial: false }],
					weight: 1,
					columnCount: 1,
				}]));
				break;

			case 'final':
				dispatch(setRoundThemes(finalThemes));
				dispatch(showThemeStack());
				break;

			case 'buzzer':
				dispatch(showText('Кто первым нажмёт кнопку?'));
				dispatch(playerStateChanged({ index: 1, state: PlayerStates.Press }));
				break;

			case 'correct':
				dispatch(showText('Правильный ответ: гиперпространство'));
				dispatch(playerStateChanged({ index: 1, state: PlayerStates.Right }));
				break;

			case 'incorrect':
				dispatch(showText('Ответ игрока проверяется'));
				dispatch(playerStateChanged({ index: 1, state: PlayerStates.Wrong }));
				break;

			case 'players':
				dispatch(showText('Счёт после третьего раунда'));
				break;

			case 'video':
			case 'audio':
				dispatch(showText(''));
				break;

			case 'question':
			default:
				dispatch(showText('Как называется воображаемая область пространства, позволяющая путешествовать быстрее света?'));
				break;
		}
	}, [dispatch, previewState]);

	React.useEffect(() => {
		const webview = window.chrome?.webview;

		if (!webview) {
			return undefined;
		}

		const onMessage = (event: Event & { data?: EditorMessage }) => {
			const message = event.data;

			if (!message) {
				return;
			}

			if (message.type === 'applyTheme') {
				const parsedTheme = parseSimulatorTheme(message.theme);

				if (parsedTheme) {
					setActiveThemeId(parsedTheme.id);
				}
			} else if (message.type === 'themeLibrary' && message.themes) {
				setLibrary(message.themes);
				setActiveThemeId(message.activeThemeId ?? 'simulator-default');
			} else if (message.type === 'editorStatus' && message.message) {
				setStatus(message.message);
			}
		};

		webview.addEventListener('message', onMessage);
		return () => webview.removeEventListener('message', onMessage);
	}, []);

	const updateTheme = (mutator: (draft: SimulatorThemeDocument) => void) => {
		const draft = cloneTheme(theme);
		mutator(draft);
		dispatch(applySimulatorTheme(draft));
		setActiveThemeId(draft.id);
		setStatus('Есть несохранённые изменения');
		postEditorMessage({ type: 'themeChanged', theme: draft });
	};

	const updateTypography = (
		field: keyof SimulatorTypographyToken,
		value: string | number,
	) => updateTheme(draft => {
		draft.tokens.typography[typographyRole] = {
			...draft.tokens.typography[typographyRole],
			[field]: value,
		} as SimulatorTypographyToken;
	});

	const save = () => postEditorMessage({ type: 'saveTheme', theme });
	const duplicate = () => postEditorMessage({ type: 'duplicateTheme', theme, name: `${theme.name} Copy` });

	const reset = () => {
		const defaultTheme = cloneTheme(defaultSimulatorTheme);
		dispatch(applySimulatorTheme(defaultTheme));
		setActiveThemeId(defaultTheme.id);
		setStatus('Загружена стандартная тема');
		postEditorMessage({ type: 'loadTheme', id: defaultTheme.id });
	};

	const exportTheme = () => {
		const blob = new Blob([JSON.stringify(theme, null, 2)], { type: 'application/json' });
		const url = URL.createObjectURL(blob);
		const anchor = document.createElement('a');
		const safeName = theme.name.replace(/[^A-Za-z0-9А-яЁё_-]+/g, '-').replace(/^-|-$/g, '') || 'theme';
		anchor.href = url;
		anchor.download = `${safeName}.theme.json`;
		anchor.click();
		URL.revokeObjectURL(url);
		setStatus('Тема экспортирована в JSON');
	};

	const importTheme = async (event: React.ChangeEvent<HTMLInputElement>) => {
		const file = event.target.files?.[0];

		if (!file) {
			return;
		}

		try {
			const parsedTheme = parseSimulatorTheme(JSON.parse(await file.text()));

			if (!parsedTheme) {
				throw new Error('Файл не соответствует текущей версии Theme Schema');
			}

			if (!postEditorMessage({ type: 'importTheme', theme: parsedTheme })) {
				dispatch(applySimulatorTheme(parsedTheme));
			}
			setStatus('Тема импортирована');
		} catch (error) {
			setStatus(error instanceof Error ? error.message : 'Не удалось импортировать тему');
		} finally {
			event.target.value = '';
		}
	};

	const selectedTypography = theme.tokens.typography[typographyRole];
	const availableFontFamilies = getSimulatorThemeFontFamilies(theme);

	const importFont = async (event: React.ChangeEvent<HTMLInputElement>) => {
		const file = event.target.files?.[0];

		if (!file) {
			return;
		}

		try {
			const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
			const mimeType = supportedFontMimeTypes[extension];

			if (!mimeType) {
				throw new Error('Поддерживаются шрифты TTF, OTF, WOFF и WOFF2');
			}

			if (file.size === 0 || file.size > MAX_SIMULATOR_FONT_FILE_SIZE) {
				throw new Error('Размер файла шрифта должен быть от 1 байта до 5 МБ');
			}

			if (Object.keys(theme.assets.fonts).length >= MAX_SIMULATOR_THEME_FONTS) {
				throw new Error(`В одной теме можно хранить не более ${MAX_SIMULATOR_THEME_FONTS} шрифтов`);
			}

			const fontFamily = createFontFamily(file.name, theme.assets.fonts);
			const dataUrl = await readFontAsDataUrl(file, mimeType);

			updateTheme(draft => {
				draft.assets.fonts[fontFamily] = dataUrl;
				draft.tokens.typography[typographyRole].fontFamily = fontFamily;
			});
			setStatus(`Шрифт «${fontFamily}» добавлен в тему`);
		} catch (error) {
			setStatus(error instanceof Error ? error.message : 'Не удалось импортировать шрифт');
		} finally {
			event.target.value = '';
		}
	};

	const removeFont = () => {
		const { fontFamily } = selectedTypography;

		if (!theme.assets.fonts[fontFamily]) {
			return;
		}

		updateTheme(draft => {
			delete draft.assets.fonts[fontFamily];

			Object.values(draft.tokens.typography).forEach(token => {
				if (token.fontFamily === fontFamily) {
					token.fontFamily = SIMULATOR_BUILT_IN_FONT_FAMILY;
				}
			});
		});
		setStatus(`Шрифт «${fontFamily}» удалён из темы`);
	};

	const mediaPlaceholder = previewState === 'video' || previewState === 'audio'
		? <div className={`themeEditorMediaPlaceholder ${previewState}`}>
			<div className='themeEditorMediaIcon'>{previewState === 'video' ? '▶' : '♫'}</div>
			<strong>{previewState === 'video' ? 'Видео 16:9' : 'Аудиовопрос'}</strong>
			<span>{previewState === 'video' ? 'Область видеоконтента' : 'Воспроизведение локального аудио'}</span>
		</div>
		: null;

	return (
		<div className='themeEditor'>
			<header className='themeEditorHeader'>
				<div>
					<strong>SImulator Theme Editor</strong>
					<span>Локальный визуальный редактор</span>
				</div>

				<select value={activeThemeId} onChange={event => postEditorMessage({ type: 'loadTheme', id: event.target.value })}>
					{library.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}
				</select>

				<div className='themeEditorActions'>
					<button type='button' onClick={reset}>Сбросить</button>
					<button type='button' onClick={() => importInput.current?.click()}>Импорт</button>
					<button type='button' onClick={exportTheme}>Экспорт</button>
					<button type='button' onClick={duplicate}>Дублировать</button>
					<button type='button' className='primary' onClick={save}>
						{theme.id === 'simulator-default' ? 'Сохранить копию' : 'Сохранить'}
					</button>
					<input ref={importInput} type='file' accept='.json,.theme.json' hidden onChange={importTheme} />
				</div>
			</header>

			<aside className='themeEditorNavigation'>
				<div className='themeEditorEyebrow'>Разделы</div>
				{editorSections.map(item => (
					<button
						type='button'
						key={item.id}
						className={section === item.id ? 'active' : ''}
						onClick={() => setSection(item.id)}>
						{item.label}
					</button>
				))}
			</aside>

			<main className='themeEditorWorkspace'>
				<div className='themeEditorPreviewToolbar'>
					<div>
						<strong>Live preview</strong>
						<span>16:9 · 1280 × 720</span>
					</div>
					<div className='themeEditorPreviewStates'>
						{previewStates.map(item => (
							<button
								type='button'
								key={item.id}
								className={previewState === item.id ? 'active' : ''}
								onClick={() => setPreviewState(item.id)}>
								{item.label}
							</button>
						))}
					</div>
				</div>

				<div className='themeEditorCanvas'>
					<div className='themeEditorViewport'>
						<AudioContextProvider>
							<ThemeStyleHost className='playersAndTable themeEditorPreviewStage'>
								<GameTable />
								<PlayersView />
								{mediaPlaceholder}
							</ThemeStyleHost>
						</AudioContextProvider>
					</div>
				</div>
			</main>

			<aside className='themeEditorInspector'>
				<div className='themeEditorEyebrow'>Свойства</div>
				<Field label='Название темы'>
					<input
						value={theme.name}
						onChange={event => updateTheme(draft => { draft.name = event.target.value; })} />
				</Field>

				{section === 'global' ? <>
					<Field label='Цвет фона'>
						<input
							type='color'
							value={toColorInput(theme.tokens.global.backgroundColor)}
							onChange={event => updateTheme(draft => {
								draft.tokens.global.backgroundColor = event.target.value;
							})} />
					</Field>
					<Field label='Фоновое изображение'>
						<input
							placeholder='Локальный путь или data URL'
							value={theme.tokens.global.backgroundImage ?? ''}
							onChange={event => updateTheme(draft => {
								draft.tokens.global.backgroundImage = event.target.value || null;
							})} />
					</Field>
					<Field label='Масштаб фона'>
						<select
							value={theme.tokens.global.backgroundFit}
							onChange={event => updateTheme(draft => {
								draft.tokens.global.backgroundFit = event.target.value as typeof draft.tokens.global.backgroundFit;
							})}>
							<option value='cover'>Заполнить</option>
							<option value='contain'>Вместить</option>
							<option value='stretch'>Растянуть</option>
							<option value='center'>По центру</option>
						</select>
					</Field>
					<Field label='Акцентный цвет'>
						<input
							type='color'
							value={toColorInput(theme.tokens.global.accentColor)}
							onChange={event => updateTheme(draft => {
								draft.tokens.global.accentColor = event.target.value;
							})} />
					</Field>
				</> : null}

				{section === 'typography' ? <>
					<Field label='Элемент'>
						<select
							value={typographyRole}
							onChange={event => setTypographyRole(event.target.value as TypographyRole)}>
							{typographyRoles.map(item => <option key={item.id} value={item.id}>{item.label}</option>)}
						</select>
					</Field>
					<Field label='Шрифт'>
						<div className='themeEditorFontControls'>
							<select
								className='themeEditorFontFamily'
								value={selectedTypography.fontFamily}
								onChange={event => updateTypography('fontFamily', event.target.value)}>
								{availableFontFamilies.map(fontFamily => (
									<option key={fontFamily} value={fontFamily}>{fontFamily}</option>
								))}
							</select>
							<div className='themeEditorFontActions'>
								<button type='button' onClick={() => fontImportInput.current?.click()}>Загрузить…</button>
								<button
									type='button'
									disabled={!theme.assets.fonts[selectedTypography.fontFamily]}
									onClick={removeFont}>
									Удалить
								</button>
							</div>
							<small>TTF, OTF, WOFF или WOFF2 · до 5 МБ · хранится внутри темы</small>
							<input
								ref={fontImportInput}
								className='themeEditorFontInput'
								type='file'
								accept='.ttf,.otf,.woff,.woff2,font/ttf,font/otf,font/woff,font/woff2'
								hidden
								onChange={importFont} />
						</div>
					</Field>
					<Field label='Размер'>
						<input
							type='number'
							min='8'
							max='240'
							value={selectedTypography.fontSize}
							onChange={event => updateTypography('fontSize', Number(event.target.value))} />
					</Field>
					<Field label='Начертание'>
						<select
							value={selectedTypography.fontWeight}
							onChange={event => updateTypography('fontWeight', Number(event.target.value))}>
							<option value='300'>Light</option>
							<option value='400'>Regular</option>
							<option value='500'>Medium</option>
							<option value='600'>Semi Bold</option>
							<option value='700'>Bold</option>
							<option value='900'>Black</option>
						</select>
					</Field>
					<Field label='Цвет'>
						<input
							type='color'
							value={toColorInput(selectedTypography.color)}
							onChange={event => updateTypography('color', event.target.value)} />
					</Field>
				</> : null}

				{section === 'board' ? <>
					<Field label='Фон ячейки'>
						<input
							value={theme.tokens.board.cellBackground}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.cellBackground = event.target.value;
							})} />
					</Field>
					<Field label='Фон названия темы'>
						<input
							value={theme.tokens.board.themeHeaderBackground}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.themeHeaderBackground = event.target.value;
							})} />
					</Field>
					<Field label='Расстояние'>
						<input
							type='number'
							min='0'
							max='40'
							value={theme.tokens.board.gap}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.gap = Number(event.target.value);
							})} />
					</Field>
					<Field label='Внутренний отступ'>
						<input
							type='number'
							min='0'
							max='80'
							value={theme.tokens.board.padding}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.padding = Number(event.target.value);
							})} />
					</Field>
					<Field label='Толщина рамки'>
						<input
							type='number'
							min='0'
							max='20'
							value={theme.tokens.board.borderWidth}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.borderWidth = Number(event.target.value);
							})} />
					</Field>
					<Field label='Цвет рамки'>
						<input
							value={theme.tokens.board.borderColor}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.borderColor = event.target.value;
							})} />
					</Field>
					<Field label='Скругление'>
						<input
							type='number'
							min='0'
							max='80'
							value={theme.tokens.board.borderRadius}
							onChange={event => updateTheme(draft => {
								draft.tokens.board.borderRadius = Number(event.target.value);
							})} />
					</Field>
				</> : null}

				{section === 'question' ? <>
					<Field label='Фон вопроса'>
						<input
							value={theme.tokens.question.background}
							onChange={event => updateTheme(draft => {
								draft.tokens.question.background = event.target.value;
							})} />
					</Field>
					<Field label='Ширина контента'>
						<input
							value={theme.tokens.question.contentWidth}
							onChange={event => updateTheme(draft => {
								draft.tokens.question.contentWidth = event.target.value;
							})} />
					</Field>
					<Field label='Отступ'>
						<input
							type='number'
							min='0'
							max='120'
							value={theme.tokens.question.padding}
							onChange={event => updateTheme(draft => {
								draft.tokens.question.padding = Number(event.target.value);
							})} />
					</Field>
					<Field label='Выравнивание'>
						<select
							value={theme.tokens.question.alignment}
							onChange={event => updateTheme(draft => {
								draft.tokens.question.alignment = event.target.value as typeof draft.tokens.question.alignment;
							})}>
							<option value='left'>Слева</option>
							<option value='center'>По центру</option>
							<option value='right'>Справа</option>
						</select>
					</Field>
					<Field label='Размер изображения'>
						<select
							value={theme.tokens.question.imageFit}
							onChange={event => updateTheme(draft => {
								draft.tokens.question.imageFit = event.target.value as typeof draft.tokens.question.imageFit;
							})}>
							<option value='contain'>Вместить</option>
							<option value='cover'>Заполнить</option>
							<option value='fill'>Растянуть</option>
						</select>
					</Field>
				</> : null}

				{section === 'players' ? <>
					<Field label='Расположение'>
						<select
							value={theme.tokens.players.layout}
							onChange={event => updateTheme(draft => {
								draft.tokens.players.layout = event.target.value as typeof draft.tokens.players.layout;
							})}>
							<option value='horizontal'>Горизонтально</option>
							<option value='vertical'>Вертикально</option>
						</select>
					</Field>
					<Field label='Расстояние'>
						<input
							type='number'
							min='0'
							max='60'
							value={theme.tokens.players.gap}
							onChange={event => updateTheme(draft => {
								draft.tokens.players.gap = Number(event.target.value);
							})} />
					</Field>
					<Field label='Фон карточки'>
						<input
							value={theme.tokens.players.cardBackground}
							onChange={event => updateTheme(draft => {
								draft.tokens.players.cardBackground = event.target.value;
							})} />
					</Field>
					<Field label='Цвет рамки'>
						<input
							value={theme.tokens.players.cardBorderColor}
							onChange={event => updateTheme(draft => {
								draft.tokens.players.cardBorderColor = event.target.value;
							})} />
					</Field>
					<Field label='Толщина рамки'>
						<input
							type='number'
							min='0'
							max='20'
							value={theme.tokens.players.cardBorderWidth}
							onChange={event => updateTheme(draft => {
								draft.tokens.players.cardBorderWidth = Number(event.target.value);
							})} />
					</Field>
					<Field label='Скругление'>
						<input
							type='number'
							min='0'
							max='80'
							value={theme.tokens.players.cardBorderRadius}
							onChange={event => updateTheme(draft => {
								draft.tokens.players.cardBorderRadius = Number(event.target.value);
							})} />
					</Field>
				</> : null}

				{theme.id !== 'simulator-default' ? (
					<button type='button' className='themeEditorDelete' onClick={() => postEditorMessage({ type: 'deleteTheme', id: theme.id })}>
						Удалить тему
					</button>
				) : null}
			</aside>

			<footer className='themeEditorStatus'>{status}</footer>
		</div>
	);
}
