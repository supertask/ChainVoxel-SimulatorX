﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;

/**
 * 構造層のためのStructureTableを実装したクラス．<br>
 * <br>
 * ChainVoxelのための構造層を実現したクラス．ChainVoxelクラスと組み合わせて使う．<br>
 * StructureTableへの操作はcreate，join，leaveの3種類をサポートしている．
 * @author kengo92i
 */
public class StructureTable {
	/**
     * グループ(gid)に属するグループメンバー(posID)を管理するためのテーブル
     */
	SortedDictionary<string, HashSet<string>> groupMembersTable; 

	/**
     * voxel(posID)が所属しているグループ(gid, ts)を管理するテーブル
     */
	SortedDictionary<string, HashSet<Group>> groupEntriesTable;

	/**
     * Structure Table のコンストラクタ
     */
	public StructureTable() {
		this.groupMembersTable = new SortedDictionary<string, HashSet<string>>(); 
		this.groupEntriesTable = new SortedDictionary<string, HashSet<Group>>();
	}

	public SortedDictionary<string, HashSet<string>> getGroupMembersTable() {
		return this.groupMembersTable;
	}

	/**
     * Structure Table にグループ(gid)を作成する．
     * 既に作成されたグループのgidの場合は実行されない．
     * @param gid グループ識別子
     * @see Operation
     */
	public void create(string gid) {
		if (groupMembersTable.ContainsKey(gid)) { // 既にグループ(gid)が存在する
			return;
		}
		groupMembersTable.Add(gid, new HashSet<string>());
	}


	/**
	 * Structure Table にグループ(gid)を作成する．
	 * 既に作成されたグループのgidの場合は実行されない．
	 * @param gid グループ識別子
	 * @see Operation
	 */
	private void destroy(string gid) {
		if (groupMembersTable.ContainsKey(gid)) { return; } // 既にグループ(gid)が存在する
		groupMembersTable.Add(gid, new HashSet<string>());

		/*
		List<Voxel> voxelList = this.getVoxelList(posID);

		// step2: 負のvoxelより古いvoxelを削除する
		for (int i = voxelList.Count - 1; i >= 0; --i) { // 先頭から削除するとイテレータがおかしくなる
			if (this.negativeVoxels[posID].getTimestamp() >= voxelList[i].getTimestamp()) {
				voxelList.RemoveAt(i); 
			}
		}
		*/
	}

	/**
     * グループ(gid)にvoxel(posID)を参加させる
     * @param ts タイムスタンプ
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @see Operation
     */
	public bool join(long ts, string posID, string gid)
	{
		this.create(gid);
		Group aGroup = new Group(gid, ts);
		if (!this.groupMembersTable.ContainsKey(gid)) { return false; }

		if (Math.Abs(this.getTimestamp(posID, gid)) >= ts) { return false; }

		// groupEntriesTable に Group(gid, ts) を追加
		if (!this.groupEntriesTable.ContainsKey(posID)) {
			this.groupEntriesTable.Add(posID, new HashSet<Group>(new GroupComparer())); //バグってた
		}
		this.groupEntriesTable[posID].Add(aGroup);

		// groupMembersTable に posID を追加
		this.groupMembersTable[gid].Add(posID);

		// タイムスタンプの値を最新の値に更新する
		long maxTs = Util.Max(ts, this.getTimestamp(posID, gid));
		this.setTimestamp(maxTs, posID, gid);
		return true;
	}

	/**
     * 指定したグループにvoxelたちを全て同時刻に参加させる
     * @param ts タイムスタンプ
     * @param posIDs voxel識別子たち
     * @param gid グループ識別子
     * @see Operation
     */
	public bool joinAll(long ts, string[] posIDs, string gid) {
		foreach (string posID in posIDs) {
			bool isJoined = this.join(ts, posID, gid);
			//Debug.Log (isJoined);
			if (!isJoined) {return false; }
		}
		return true;
	}

	/**
     * グループ(gid)からvoxel(posID)を脱退させる
     * @param sid site識別子
     * @param ts タイムスタンプ
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @see Operation
     */
	public bool leave(int sid, long ts, string posID, string gid) {
		Group aGroup = new Group(gid, ts);
		if (!this.groupEntriesTable.ContainsKey(posID)) { return false; }
		HashSet<Group> groupEntriesSet = this.groupEntriesTable[posID];

		long latestTs = this.getTimestamp (posID, gid);
		//Debug.Log ("second current timestamp: " + ts);
		//Debug.Log("latest timestamp: " + latestTs); //tsは現在時刻なのでtsの方が大きくなるのが普通
		//Debug.Log(Math.Abs(this.getTimestamp(posID, gid)) >= ts);
		if (!groupEntriesSet.Contains(aGroup) || Math.Abs(this.getTimestamp(posID, gid)) >= ts) {
			return false;
		} 
		this.groupMembersTable[gid].Remove(posID);
		//Debug.Log ("Removed!!!");

		// タイムスタンプの更新 + tombstone化
		long minTs = Util.Min(-1L * ts, this.getTimestamp(posID, gid));
		this.setTimestamp(minTs, posID, gid);
		return true;
	}

	/**
     * グループ(gid)からvoxel(posID)たちをすべて同時刻に脱退させる
     * @param sid site識別子
     * @param ts タイムスタンプ
     * @param posIDs voxel識別子たち
     * @param gid グループ識別子
     * @see Operation
     */
	public bool leaveAll(int sid, long ts, string gid, string[] posIDs) {
		foreach (string posID in posIDs) {
			bool isLeft = this.leave(sid, ts, posID, gid);
			//Debug.Log(gid + " " + ts + " "  + posID + " " + isLeft);
			if (!isLeft) { return false; }
		}
		return true;
	}

	/**
     * posIDに関連したグループ(gid)のタイムスタンプを取得
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @return posIDが関連しているgidのタイムスタンプ，存在しない場合は0を返す．
     */
	private long getTimestamp(string posID, string gid) {
		if (!this.groupEntriesTable.ContainsKey(posID)) { return 0; }
		HashSet<Group> groupEntries = this.groupEntriesTable[posID];

		foreach (Group aGroup in groupEntries) {
			if (!aGroup.getGroupId().Equals(gid)) {
				continue;
			}
			return aGroup.getTimestamp();
		}
		return 0;
	}

	/**
     * posIDに関連したグループ(gid)のタイムスタンプを設定
     * @param ts 更新するタイムスタンプ
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @return 値の更新に成功した場合はtrueを返す．失敗した場合はfalseを返す．
     */
	private bool setTimestamp(long ts, string posID, string gid) {
		if (!this.groupEntriesTable.ContainsKey(posID)) { return false; }
		HashSet<Group> groupEntries = this.groupEntriesTable[posID];

		foreach (Group aGroup in groupEntries) {
			if (aGroup.getGroupId().Equals(gid)) {
				aGroup.setTimestamp(ts); 
				return true;
			} 
		}
		return false;
	}

	/**
     * posIDに関連したグループ(gid)が墓石か判定する
	 * Not used
	 * @authoer Kengo
     * @param posID voxel識別子
     * @param gid グループ識別子
     * @return 墓石ならtrueを返す．それ以外はfalseを返す．
     */
	private bool isTombstone(string posID, string gid) {
		return this.getTimestamp(posID, gid) < 0; 
	}

	/**
     * 指定したvoxelがグループ化中であるか判定する．
	 * @authoer Kengo
     * @param posID voxel識別子
     * @return グループ化中ならばtrue，そうでないならfalseを返す．
     */
	public bool isGrouping(string posID) {
		if (!this.groupEntriesTable.ContainsKey(posID)) { return false; }
		HashSet<Group> groupEntries = this.groupEntriesTable[posID];

		foreach (Group aGroup in groupEntries) {
			if (aGroup.getTimestamp() > 0) {
				return true;
			}
		}
		return false;
	}

	/*
	public bool isGroupingAll(string[] posIDs) {
		foreach(string posID in posIDs){
			if (this.isGrouping(posID)) return true;
		}
		return false;
	}
	*/
	public bool isGroupingAll(string[] posIDs) {
		foreach(string posID in posIDs){
			if (!this.isGrouping(posID)) return false;
		}
		return true; //When all voxels are grouped
	}

	/**
	 * NEW!!
	 * posIDに紐付いたGroupのハッシュセット（リスト）を応答する。
	 * @param posID voxel識別子
	 * @return gid グループIDのハッシュセット
	 */
	public HashSet<Group> getGroups(string posID) {
		if (this.groupEntriesTable.ContainsKey (posID)) {
			return this.groupEntriesTable [posID];
		}
		else { return null; }
	}

	/**
	 * NEW!!
	 * gidに紐付いたposIDのハッシュセット（リスト）を応答する。
	 * @param gid グループID
	 * @return posIDのハッシュセット
	 */
	public string[] getPosIDs(string gid) {
		if (this.groupMembersTable.ContainsKey (gid)) {
			HashSet<string> hashPosIDs = this.groupMembersTable [gid];
			string[] posIDs = new string[hashPosIDs.Count];
			hashPosIDs.CopyTo(posIDs);
			return posIDs;
		}
		else { return null; }
	}

	/**
     * StructureTableの状態を確認するための文字列を取得する
     * 各siteのStructureTableの状態が同じであるか確認するために使用する
     * @return StructureTableの状態を示す文字列
     */
	public string getStatusString() {
		string statusString = "";
		statusString += "groupMembersTable:\n";
		foreach (KeyValuePair<string,HashSet<string>> entry in this.groupMembersTable) {
			statusString +=  "gid=" + entry.Key + " |\t"; 
			foreach (string gid in entry.Value) {
				statusString += gid + "\t";
			}
			statusString += "\n";
		}
		statusString += "\n";

		statusString += "groupEntriesTable:\n";
		foreach (KeyValuePair<string,HashSet<Group>> entry in this.groupEntriesTable) {
			statusString += entry.Key + " |\t";
			foreach (Group g in entry.Value) {
				statusString += g.ToString() + "\t";
			}
			statusString += "\n";
		}
		statusString += "\n\n";

		return statusString;
	}

	/**
     * Structure Table の状態を出力する
     * @param dumpMsg ダンプメッセージ
     */
	public void show(string dumpMsg) {
		Debug.Log(dumpMsg);
		Debug.Log(this.getStatusString());
	}

	public void show() {
		Debug.Log(this.getStatusString());
	}

	public long getMemory() {
		long memorySize = 0L;
		foreach (KeyValuePair<string,HashSet<string>> p in this.groupMembersTable) {
			memorySize += p.Key.Length * 1; //1文字=1バイト
			foreach (string gid in p.Value) {
				memorySize += p.Key.Length * 1; //1文字=1バイト
			}
		}
		foreach (KeyValuePair<string,HashSet<Group>> p in this.groupEntriesTable) {
			memorySize += p.Key.Length * 1; //1文字=1バイト
			foreach (Group g in p.Value) {
				memorySize += g.getMemory();
			}
		}
		return memorySize;
	}


	/**
	 * Test a StructureTable class.
	 */
	public static void Test() {
		/*
		StructureTable stt = new StructureTable ();	

		List<string> gids =  new List<string>();
		List<string> posIDs = new List<string>(){"1:1:1", "1:2:3", "5:1:9", "7:8:0", "9:4:1"};
		for (int i = 0; i < 5; ++i) {
			gids.Add(i.ToString()); //UUID.randomUUID().tostring()
		}
		*/

		string res="";
		StructureTable stt = new StructureTable ();	

		List<string> gids =  new List<string>();
		List<string> posIDs = new List<string>(){"1:1:1", "1:2:3", "5:1:9", "7:8:0", "9:4:1"};
		for (int i = 0; i < 5; ++i) {
			gids.Add(i.ToString()); //UUID.randomUUID().tostring()
		}

		/*
		stt.create(gids[0]);	
		stt.create(gids[0]); // 既にあるグループを作成

		stt.join(1L, posIDs[0], gids[0]); //Bug
		stt.join(2L, posIDs[1], gids[0]);
		stt.join(3L, posIDs[2], gids[0]);
		stt.leave(1, 4L, posIDs[1], gids[0]);
		stt.show("正常な参加・脱退");

		res = "";
		stt.join(5L, posIDs[0], gids[1]); // 存在しないグループへの参加
		stt.leave(1, 5L, posIDs[1], gids[1]); // 参加していないグループからの脱退
		stt.show("不正な参加・脱退");

		stt.create(gids[1]);
		stt.create(gids[2]);
		stt.create(gids[3]);
		stt.join(6L, posIDs[0], gids[1]);
		stt.join(7L, posIDs[3], gids[3]);
		stt.leave(1, 8L, posIDs[3], gids[3]);
		stt.show("グループの追加");

		stt.join(8L, posIDs[1], gids[2]);
		stt.join(9L, posIDs[1], gids[2]);
		stt.join(7L, posIDs[1], gids[2]);
		stt.leave(1, 8L, posIDs[0], gids[0]);
		stt.leave(1, 10L, posIDs[0], gids[0]);
		stt.leave(1, 11L, posIDs[0], gids[1]);
		stt.show("複数のjoin・leaveの収束結果");
		*/


	}

}
