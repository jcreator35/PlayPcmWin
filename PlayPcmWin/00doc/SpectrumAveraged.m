% Plots the frequency spectrum of 2.8224MHz mono PCM 1.000kHz sine signal,
%   time domain averaged and frequency domain averaged.
%   At least 4 minutes of tone signal is needed. 
SOUND_FILENAME      = 'D:/audio/1kHzSine_4min_2_8MHz_mono.wav';

% 1kHz signal takes 2822.4 samples.
% 1kHz signal x 5 period takes 2822.4 * 5 = 14112 samples.
FFT_LEN             = 14112 * 32;
FREQUENCY_AVG_COUNT = 32;
TIME_AVG_COUNT      = 32;

% 1248 == delay samples of the filter now testing: first zero cross sample pos of sine signal.
FILTER_DELAY_SAMPLES = 1248;

% Most suitable window for this purpose is Hann.
w = hann(FFT_LEN);
wSum = 0.0;
for i=1:FFT_LEN
    wSum = wSum + w(i);
end
wSum = wSum / FFT_LEN;

pos = 1 + FILTER_DELAY_SAMPLES + FFT_LEN;
% pos = 1;

fAcc = zeros([FFT_LEN/2+1 1]);
for p=1:FREQUENCY_AVG_COUNT
    tAcc = zeros([FFT_LEN 1]);
    for t=1:TIME_AVG_COUNT
        [x,fs] = audioread(SOUND_FILENAME,[pos,pos+FFT_LEN-1]);
        pos = pos + FFT_LEN;
        tAcc = tAcc + x;
    end
    tAvg = tAcc / TIME_AVG_COUNT;
    
    Y = fft(tAvg .* w);
    P2 = abs(Y/FFT_LEN); % complex to real.
    P1 = P2(1:FFT_LEN/2+1);
    P1(2:end-1) = 2*P1(2:end-1);
    P1 = P1 / wSum; % compensate attenuation of window function.
    fAcc = fAcc + P1;
end

fAvg = fAcc / FREQUENCY_AVG_COUNT;

frequencies = fs*(0:(FFT_LEN/2))/FFT_LEN;

semilogx(frequencies,mag2db(fAvg))
  grid on
  grid minor
  axis([10 inf -inf 0])
xlabel('Frequency (Hz)')
ylabel('Magnitude(dBFS)')
title('Frequency Response')
