x64\Release\NumaEffectTest2 printconfig

for ($i=0; $i -lt 10; $i++) {
    sleep 5 
    x64\Release\NumaEffectTest2 malloctest 8192
}

sleep 5
x64\Release\NumaEffectTest2 benchmark 8192

pause
