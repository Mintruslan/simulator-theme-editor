import ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import runCore from './LibraryCore';
import React from 'react';
import PlayersView from './components/game/PlayersView/PlayersView';
import GameTable from './components/gameTable/GameTable/GameTable';
import IGameClient from './client/game/IGameClient';
import AudioController from './components/common/AudioController/AudioController';
import { showLogo } from './state/tableSlice';
import { setAppSound } from './state/settingsSlice';
import QRCodeView from './components/panels/QRCodeView/QRCodeView';
import { AudioContextProvider } from './contexts/AudioContextProvider';
import ThemeStyleHost from './presentation/ThemeStyleHost';
import ThemeEditor from './presentation/editor/ThemeEditor';

import './scss/style.scss';

export { defaultSimulatorTheme } from './model/SimulatorTheme';

export function run(elementId: string, game?: IGameClient): void {
	const host = document.getElementById(elementId);

	if (!host) {
		console.error('Host element not found!');
		return;
	}

	const store = runCore(game);

	store.dispatch(showLogo());
	store.dispatch(setAppSound(true));

	ReactDOM.render(
		<Provider store={store}>
			<AudioContextProvider>
				<ThemeStyleHost className='playersAndTable'>
					<PlayersView />
					<GameTable />
					<AudioController />
					<QRCodeView />
				</ThemeStyleHost>
			</AudioContextProvider>
		</Provider>,
		host
	);
}

export function runThemeEditor(elementId: string): void {
	const host = document.getElementById(elementId);

	if (!host) {
		console.error('Theme Editor host element not found!');
		return;
	}

	const store = runCore();
	store.dispatch(setAppSound(false));

	ReactDOM.render(
		<Provider store={store}>
			<ThemeEditor />
		</Provider>,
		host
	);
}
