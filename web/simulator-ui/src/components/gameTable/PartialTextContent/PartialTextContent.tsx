import React, { useState, useEffect, useRef } from 'react';
import { useAppSelector } from '../../../state/hooks';
import fitElement from '../../../utils/fitElement';
import { toTypographyStyle } from '../../../presentation/ThemeStyleHost';

import './PartialTextContent.scss';

export default function PartialTextContent() {
	const divRef = useRef<HTMLDivElement>(null);
	const text = useAppSelector(state => state.table.text + state.table.tail);
	const totalLength = useAppSelector(state => state.table.text.length);
	const textVersion = useAppSelector(state => state.table.textVersion);
	const readingSpeed = useAppSelector(state => state.room2.settings.readingSpeed);
	const isGamePaused = useAppSelector(state => state.room2.stage.isGamePaused);
	const typography = useAppSelector(state => state.settings.simulatorTheme.tokens.typography.questionText);

	const [visibleLength, setVisibleLength] = useState(0);
	const totalLengthRef = useRef(totalLength);
	const readingSpeedRef = useRef(readingSpeed);
	const isGamePausedRef = useRef(isGamePaused);

	useEffect(() => {
		if (totalLength !== 0) {
			return;
		}

		if (divRef.current) {
			divRef.current.style.fontSize = '';
			fitElement(divRef.current, typography.fontSize);
			const { fontSize } = window.getComputedStyle(divRef.current);
			divRef.current.style.fontSize = (parseFloat(fontSize) * 0.95) + 'px'; // Adjust font size slightly for better fit
		}
	}, [text, totalLength, typography]);

	useEffect(() => {
		setVisibleLength(0);
	}, [textVersion]);

	useEffect(() => {
		totalLengthRef.current = totalLength;
		readingSpeedRef.current = readingSpeed;
		isGamePausedRef.current = isGamePaused;
	}, [totalLength, readingSpeed, isGamePaused]);

	useEffect(() => {
		const interval = window.setInterval(
			() => {
				if (isGamePausedRef.current) {
					return;
				}

				setVisibleLength(prev => {
					const currentTotalLength = totalLengthRef.current;

					if (prev >= currentTotalLength) {
						return prev;
					}

					return Math.min(prev + (readingSpeedRef.current / 10), currentTotalLength);
				});
			},
			100
		);

		return () => {
			window.clearInterval(interval);
		};
	}, []);

	const visibleText = text.slice(0, visibleLength);
	const hiddenText = text.slice(visibleLength);

	return (
		<div className='textHost'>
			<div ref={divRef} className="tableText nonAligned" style={toTypographyStyle(typography)}>
				<span>
					<span className="animatablePartialCharacter">{visibleText}</span>
					<span className="invisible">{hiddenText}</span>
				</span>
			</div>
		</div>
	);
}
