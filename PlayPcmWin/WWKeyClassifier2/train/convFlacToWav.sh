for i in `seq 1 555`;do
  ((c=i%12)); ((c==11)) && wait
  sox  ../Scarlatti_ConcertPitch/K$i.flac -b 16 -r 44100 -c 1 K$i.wav &
done
