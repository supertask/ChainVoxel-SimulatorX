﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChainXView 
{
	private ChainXModel model;

	public ChainXView(ChainXModel model)
	{
		this.model = model;
	}

	public void Update() {
		GameObject anObject = GameObject.Find("DebugLog/Viewport/Content");
		Text log = anObject.GetComponent<Text>();
		log.text = this.model.GetLog(); //this.showStructureTable();	
	}

	public string showStructureTable()
	{
		string res="";
		StructureTable stt = new StructureTable ();	

		List<string> gids =  new List<string>();
		List<string> posIDs = new List<string>(){"1:1:1", "1:2:3", "5:1:9", "7:8:0", "9:4:1"};
		for (int i = 0; i < 5; ++i) {
			gids.Add(i.ToString()); //UUID.randomUUID().tostring()
		}

		stt.create(gids[0]);	
		stt.create(gids[0]); // 既にあるグループを作成

		stt.join(1L, posIDs[0], gids[0]); //Bug
		stt.join(2L, posIDs[1], gids[0]);
		stt.join(3L, posIDs[2], gids[0]);
		stt.leave(1, 4L, posIDs[1], gids[0]);
		//res += stt.getStatusString();

		res = "";
		stt.join(5L, posIDs[0], gids[1]); // 存在しないグループへの参加
		stt.leave(1, 5L, posIDs[1], gids[1]); // 参加していないグループからの脱退
		//res += stt.getStatusString(); //不正な参加、脱退


		/* Bug */
		stt.create(gids[1]);
		stt.create(gids[2]);
		stt.create(gids[3]);
		stt.join(6L, posIDs[0], gids[1]);
		stt.join(7L, posIDs[3], gids[3]);
		stt.leave(1, 8L, posIDs[3], gids[3]);
		//res += stt.getStatusString(); //グループの追加

		stt.join(8L, posIDs[1], gids[2]);
		stt.join(9L, posIDs[1], gids[2]);
		stt.join(7L, posIDs[1], gids[2]);
		stt.leave(1, 8L, posIDs[0], gids[0]);
		stt.leave(1, 10L, posIDs[0], gids[0]);
		stt.leave(1, 11L, posIDs[0], gids[1]);
		res += stt.getStatusString(); //複数のjoin・leaveの収束結果
		Debug.Log(res);

		return res;
	}

}