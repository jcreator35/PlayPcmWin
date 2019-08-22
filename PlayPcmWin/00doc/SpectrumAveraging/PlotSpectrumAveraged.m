% Plot audio steady state signal spectrum.
% time domain averaged and frequency domain averaged.
% @param timeAvg time domain average count (coherent averaging)
% @param freqAvg freq domain average count (power averaging)
function PlotSpectrumAveraged(path, dispName, fftLen, startDiscardSamples, timeAvg, freqAvg)
    % Most suitable window for this purpose is Hann.
    w = hann(fftLen);
    wSum = 0.0;
    for i=1:fftLen
        wSum = wSum + w(i);
    end
    wSum = wSum / fftLen;

    pos = 1 + startDiscardSamples + fftLen;
    % pos = 1;

    fAcc = zeros([fftLen/2+1 1]);
    for p=1:freqAvg
        tAcc = zeros([fftLen 1]);
        for t=1:timeAvg
            [x,fs] = audioread(path,[pos,pos+fftLen-1]);
            pos = pos + fftLen;
            tAcc = tAcc + x;
        end
        tAvg = tAcc / timeAvg;

        Y = fft(tAvg .* w);
        P2 = abs(Y/fftLen); % complex to real.
        P1 = P2(1:fftLen/2+1);
        P1(2:end-1) = 2*P1(2:end-1);
        P1 = P1 / wSum; % compensate attenuation of window function.
        fAcc = fAcc + P1;
    end

    fAvg = fAcc / freqAvg;

    frequencies = fs*(0:(fftLen/2))/fftLen;

    semilogx(frequencies,mag2db(fAvg), 'DisplayName', dispName)
    grid on
    grid minor
    axis([10 inf -inf 0]);
    xlabel('Frequency (Hz)');
    ylabel('Magnitude(dBFS)');
end



