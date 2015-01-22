chcp 1251
set name=MyStrategy

call compile-cs-vs.bat

move .\MyStrategy.mono.exe \Local-runner

cd Local-runner

#call local-runner
