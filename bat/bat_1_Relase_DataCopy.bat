rem Debugの確認環境にあるデータを、Releaseにコピー(ミラー)します
rem robocopy "../project/AssistVRoid\bin\Debug\data" "../project/AssistVRoid\bin\Release\data" /MIR /R:0 /W:0 /NP /XJD /XJF

rem 不足している可能性のある DXライブラリのDLLをコピーしておく
copy "../project/AssistVRoid\bin\Debug\init.txt" "../project/AssistVRoid\bin\Release\init.txt" /Y
copy "../project/AssistVRoid\bin\Debug\config.txt" "../project/AssistVRoid\bin\Release\config.txt" /Y