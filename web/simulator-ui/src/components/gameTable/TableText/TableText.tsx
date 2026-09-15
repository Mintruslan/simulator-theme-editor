import * as React from 'react';
import State from '../../../state/State';
import { connect } from 'react-redux';
import AutoSizedText from '../../common/AutoSizedText/AutoSizedText';
import ClickableAnswer from '../../common/ClickableAnswer/ClickableAnswer';
import { useAppSelector } from '../../../state/hooks';
import { toTypographyStyle } from '../../../presentation/ThemeStyleHost';

import './TableText.scss';

interface TableTextProps {
	text: string;
	isAnswer: boolean;
}

const mapStateToProps = (state: State) => ({
	text: state.table.text,
	isAnswer: state.table.isAnswer,
});

export function TableText(props: TableTextProps) {
	const typography = useAppSelector((state) => state.settings.simulatorTheme.tokens.typography);
	const typographyToken = props.isAnswer ? typography.answerText : typography.questionText;

	return (
		<AutoSizedText
			className="tableText fadeIn tableTextCenter margined"
			maxFontSize={typographyToken.fontSize}
			style={toTypographyStyle(typographyToken)}>
			{props.isAnswer ? <ClickableAnswer text={props.text} /> : props.text}
		</AutoSizedText>
	);
}

export default connect(mapStateToProps)(TableText);
