@echo off
echo Lancement dial SonarScanner...
SonarScanner.MSBuild.exe begin /k:"ExecutionFlowHistoryViewer" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="sqp_005034d31a94977653ce99e496fcd32cd3da1fa8"

echo Compilation dial l'projet...
MSBuild.exe ExecutionFlowHistoryViewer.sln /t:Rebuild

echo Envoi dial les resultats...
SonarScanner.MSBuild.exe end /d:sonar.token="sqp_005034d31a94977653ce99e496fcd32cd3da1fa8"
pause