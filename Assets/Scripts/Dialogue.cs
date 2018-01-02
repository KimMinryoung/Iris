﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Dialogue {

	public static DialogueDisplay dd;
	public static DialogueManager dm;

	string label = null;
	Action NameBox = NullNameBox;
	Action PortraitBox = NullPortraitBox;
	Action Text = NullText;
	Action Effect = NullEffect;
	Action Branch = NullBranch;
	Func<bool> Condition = NullCondition;
	Action ChangeValue = NullChangeValue;
	Action DontWaitInput = NullDontWaitInput;

	public void LoadDialogueLine(string line, Dictionary<string,int> comparedVariables){

		try{
			string[] labelParsed = line.Split(']');

			string lineWithoutLabel;

			if(labelParsed.Length==1){
				label = null;
				lineWithoutLabel=labelParsed[0];
			}
			else{
				label = labelParsed[0].Substring("[".Length);
				DontWaitInput = TrueDontWaitInput;
				return;
			}

			string[] parts = lineWithoutLabel.Split('\t');

			string dtype = parts[0];

			if (dtype == "*"){
				string commandType = parts[1];
				string commandObject = parts[2];
				LoadEffectCommand(commandType, commandObject);
			}
			else if (dtype == "->" || dtype == "?"){
				string label = parts[2];
				LoadBranch(label);
				if(dtype == "?"){
					string compareText = parts[1];
					LoadCondition(compareText, comparedVariables);
				}
			}
			else if(dtype == "+="){
				string targetStat = parts[1];
				int addedValue = Convert.ToInt32 (parts[2]);
				LoadAddValue(targetStat, addedValue, comparedVariables);
			}
			else{
				string name=dtype;
				if(name.Length != 0){
					LoadNameBox(name);
				}
				else{
					NameBox = EmptyNameBox;
				}
				string portraitFileName = parts[1];
				LoadPortraitBox(portraitFileName);
				string dialogueText=parts[2].Replace('^','\n');
				LoadTextBox(dialogueText);
			}

			if(dtype == "*" || dtype == "+="){
				DontWaitInput = TrueDontWaitInput;
			}
		}
		catch (Exception e){
			Debug.LogError("Parse error with " + line);
			Debug.LogException(e);
			throw e;
		}
	}

	void LoadEffectCommand(string commandType,string commandObject){
		if (commandType == "사라져") {
			LoadEffectDisappear (commandObject);
		} else if (commandType == "스크립트") {
			LoadEffectScript (commandObject);
		} else if (commandType == "타이틀로") {
			LoadToTheTitle ();
		} else if (commandType == "배경") {
			LoadEffectBackground (commandObject);
		} else if (commandType == "일러") {
			LoadEffectIllust (commandObject);
		} else if (commandType == "배경음") {
			LoadEffectBGM (commandObject);
		} else if (commandType == "효과음") {
			LoadEffectSE (commandObject);
		} else if (commandType == "흔들어") {
			LoadEffectShaking ();
		} else {
			Debug.LogError ("undefined effectType : " + commandType);
		}
	}
	void LoadEffectDisappear(string commandObject){
		if(commandObject=="배경"){
			Effect = () => {
				dd.RemoveBackgroundSprite();
			};
		}
		else if(commandObject=="일러"){
			Effect = () => {
				dd.RemoveIllustSprite();
			};
		}
		else if(commandObject=="배경음"){
			Effect = () => {
				SoundManager.Instance.EndBGM();
			};
		}
	}
	void LoadEffectScript(string commandObject){
		Effect = () => {
			dm.LoadDialogueFile(commandObject, null, DialogueManager.NoReplace, DialogueManager.emptyCV);
		};
	}
	void LoadToTheTitle(){
		Effect = () => {
			SceneManager.LoadScene("Title");
		};
	}
	void LoadEffectBackground(string commandObject){
		Effect = () => {
			dd.PutBackgroundSprite(commandObject);
		};
	}
	void LoadEffectIllust(string commandObject){
		Effect = () => {
			dd.PutIllustSprite(commandObject);
		};
	}
	void LoadEffectBGM(string commandObject){
		Effect = () => {
			SoundManager.Instance.PlayBGM(commandObject);
		};
	}
	void LoadEffectSE(string commandObject){
		Effect = () => {
			SoundManager.Instance.PlaySE(commandObject);
		};
	}
	void LoadEffectShaking(){
		Effect = () => {
			dd.StartShaking (2);
		};
	}
	void LoadBranch(string destinyLabel){
		Branch = () => {
			bool success = false;
			Dialogue line_;
			for(int i=0;i<dm.dialogues.Count;i++){
				line_ = dm.dialogues[i];
				if(line_.label == destinyLabel){
					dm.lineNum = i;
					success = true;
					break;
				}
			}
			if(!success){
				Debug.Log("label '"+destinyLabel+"' 못 찾아 브랜치 실패");
				DontWaitInput = TrueDontWaitInput;
				return;
			}
			dm.ExecutePresentLine();
		};
	}
	void LoadCondition(string compareText, Dictionary<string, int> comparedVariables){
		Condition=()=>{
			string[] tokens = compareText.Split (' ');

			int targetValue = comparedVariables [tokens[0]];
			string compareSymbol = tokens [1];
			int referenceValue = Convert.ToInt32 (tokens [2]);

			bool compareResult = Util.Compare (targetValue, referenceValue, compareSymbol);
			return compareResult;
		};
	}
	void LoadAddValue(string targetStat, int addedValue, Dictionary<string, int> comparedVariables){
		ChangeValue = () => {
			comparedVariables[targetStat] += addedValue;
		};
	}
	void LoadNameBox(string name){
		NameBox = () =>{
			dd.EnableNameBox();
			dd.PutNameText(name);
		};
	}
	void LoadPortraitBox(string portraitFileName){
		if (portraitFileName.Contains ("^")) {
			PortraitBox = () => {
				dd.BlackenPortraitSprite ();
				dd.PutPortraitSprite (portraitFileName.Replace("^", ""));
			};
		} else {
			PortraitBox = () => {
				dd.BrightenPortraitSprite ();
				dd.PutPortraitSprite (portraitFileName);
			};
		}
	}
	void LoadTextBox(string dialogueText){
		string displayedText = "";
		string[] textLines=dialogueText.Split('\t');
		foreach(string textline in textLines){
			displayedText += textline+"\n";
		}
		displayedText = displayedText.Substring(0,displayedText.Length-1);
		Text = () => {
			dd.EnableTextBox();
			dd.PutTextText(displayedText);
		};
	}

	public void LoadMessageLine(string line){
		NameBox = EmptyNameBox;
		Text = () => {
			dd.EnableTextBox();
			dd.PutTextText(line);
		};
	}

	public void ExecuteDialogue(){
		NameBox ();
		PortraitBox ();
		Text ();
		Effect ();
		ConditionAndBranch ();
		ChangeValue ();
		DontWaitInput ();
	}

	void ConditionAndBranch(){
		bool result = Condition ();
		if (result)
			Branch ();
		else
			DontWaitInput = TrueDontWaitInput;
	}

	static Action NullNameBox = () => {
		//don't change current name box
		//do nothing
	};
	static Action EmptyNameBox = () => {
		dd.DisableNameBox();
		dd.PutNameText(null);
	};
	static Action NullText = () => {
		//don't change current text
		//do nothing
	};
	/*static Action EmptyText = () => {
		dd.DisableTextBox();
		dd.PutTextText(null);
	};*/
	static Action NullPortraitBox = () => {
		dd.RemovePortraitSprite();
	};
	static Action NullEffect = () => {
		//do nothing
	};
	static Action NullBranch = () => {
		//do nothing
	};
	static Func<bool> NullCondition = () => {//this is called for non-conditioned branch
		return true;
	};
	static Action NullChangeValue = () => {
		//do nothing
	};
	static Action NullDontWaitInput = () => {
		//do nothing
	};
	static Action TrueDontWaitInput = () => {
		dm.ToNextLine();
	};
}
