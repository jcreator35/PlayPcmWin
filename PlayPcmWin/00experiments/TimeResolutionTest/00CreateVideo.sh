#!/bin/bash -x

mkdir a

for x in $(seq 0 900)
do
	i=$(printf '%08d' "$x")
	montage -geometry 512x256+10+10 -tile 2x3 2bit/$i.png 3bit/$i.png 4bit/$i.png 5bit/$i.png 6bit/$i.png 8bit/$i.png -title "Digitized -6dBFS Square Wave" a/$i.png&

	m=$(($x%16))
	if [ $m -eq 15 ];
	then
		wait;
	fi
done

ffmpeg -r 30 -i a/%08d.png -b 100M sw100M.avi
ffmpeg -i sw100M.avi sw.mp4


