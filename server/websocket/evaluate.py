#!/usr/bin/env python
# -*- coding:utf-8 -*-

import sys
import os
import subprocess
import time

RECORDED_OPS_DIR = "recorded_operations"
EVALUATED_DATA_DIR = "evaluated_data"

# 評価5. 操作数 vs メモリ数
# 評価6. サイト数 vs メモリ数
#TODO(Tasuku): 12/27以降に実装


# 操作の刻み幅
# 10なら10操作刻みで計測を行う
STRIDE_OPERATIONS = 5


if len(sys.argv) < 3:
    sys.exit(1)

modules, target = sys.argv[1:3]


def exec_process(line):
    p = subprocess.Popen(line, shell=True)
    try:
        while p.poll() is None:
            time.sleep(0.1)
    except KeyboardInterrupt:
        p.terminate()
        raise
        #sys.exit(1)
    p.wait()


def evaluate_operations():
    """ 操作数とステップ数/メッセージ数を各アルゴリズム(ChainVoxel, Raft)ごとに
        測定し，出力する
    """
    print "Evaluate operations!!"

    #
    # 全てのサイトの操作数の最小値を取得
    #
    min_num_lines = 10**9
    num_of_sites = 0
    for i, filename in enumerate(os.listdir(RECORDED_OPS_DIR)):
        if filename == (str(i) + ".txt"):
            filepath = os.path.join(RECORDED_OPS_DIR, filename)
            num_lines = sum(1 for line in open(filepath)) 
            min_num_lines = min(num_lines, min_num_lines)
            num_of_sites+=1
    print "操作数の最小値:", min_num_lines

    # 操作の刻みのインデックス
    i = 1

    while (i * STRIDE_OPERATIONS) < min_num_lines:
        # 各サイトの操作数
        operations_on_each_site = i * STRIDE_OPERATIONS
        
        # 全ての操作数 = 各サイトの操作数 * サイト数
        total_operations = operations_on_each_site * num_of_sites 

        # 操作数を変動させたり，操作時間間隔を縮めたり，操作を改変する．
        line = ""
        line += "python modifier.py fast %s;\n" % operations_on_each_site

        # ある操作数の時のRaftのステップ数，メッセージ数をログファイルに出力
        # sites=サイト数，operations=各サイトの操作数
        line += "java -cp %s %s RAFT %s %s;\n" \
            % (modules, target, num_of_sites, operations_on_each_site)

        # ある操作数の時のChainVoxelのステップ数，メッセージ数をログファイルに出力
        # sites=サイト数，operations=各サイトの操作数
        line += "java -cp %s %s CHAINVOXEL %s %s;\n" \
            % (modules, target, num_of_sites, operations_on_each_site)
        print line
        exec_process(line)
        i+=1


def evaluate_sites():
    """
    """
    print "Evaluate sites!!"
    num_of_sites = 0
    for i, filename in enumerate(os.listdir(RECORDED_OPS_DIR)):
        if filename == (str(i) + ".txt"):
            num_of_sites += 1

    #
    # サイト数を指定してステップ数，メッセージ数を計測する
    #
    exec_process("python modifier.py fast;")
    line = ""
    for i in range(num_of_sites):
        line = ""
        line += "java -cp %s %s RAFT %s;\n" % (modules, target, i+1) #1オリジン
        line += "java -cp %s %s CHAINVOXEL %s;\n" % (modules, target, i+1) #1オリジン
        print line
        exec_process(line)



evaluate_operations()
print
evaluate_sites()