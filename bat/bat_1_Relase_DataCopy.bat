rem Debug�̊m�F���ɂ���f�[�^���ARelease�ɃR�s�[(�~���[)���܂�
rem robocopy "../project/AssistVRoid\bin\Debug\data" "../project/AssistVRoid\bin\Release\data" /MIR /R:0 /W:0 /NP /XJD /XJF

rem �s�����Ă���\���̂��� DX���C�u������DLL���R�s�[���Ă���
copy "../project/AssistVRoid\bin\Debug\init.txt" "../project/AssistVRoid\bin\Release\init.txt" /Y
copy "../project/AssistVRoid\bin\Debug\config.txt" "../project/AssistVRoid\bin\Release\config.txt" /Y