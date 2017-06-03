% uses delta sigma toolbox

figure('Name', 'ループフィルターのNTFのF特', 'NumberTitle', 'off');
osr=64;
originalSR=44100;

for order=2:1:9  % 2,3,4,5,6,7,8,9
    opt=1;
    H = synthesizeNTF(order, osr, opt);
    nH = H.Z{1, 1};
    dH = H.P{1, 1};

    fprintf('order=%d\n', order);
    fprintf(    '    numerator roots=\n');
    for i=1:size(nH,1)
        fprintf('        %s,\n', num2str(nH(i),17));
    end % i
    fprintf(    '    denominator roots=\n');
    for i=1:size(dH,1)
        fprintf('        %s,\n', num2str(dH(i),17));
    end % i
    
%   figure('Name', num2str(order), 'NumberTitle', 'off');
%   plotPZ(H);
    
    f=logspace(log10(10/(originalSR*osr)), log10(0.5), 1000); % 10Hzから2.8MHz/2まで対数軸で等間隔の値の1000個のベクトル。
    z=exp(2i*pi*f);
    
    tf=evalTF(H, z);
    semilogx(f*originalSR*osr, dbv(tf));
    hold on % 1個のグラフにプロットする。

    sigma_H = dbv(rmsGain(H, 0, 0.5/osr));
    fprintf('    sigma_H=%f\n', sigma_H);
end % order

% 22.05kHzに線を引く。
line([22050 22050], [20 -180], 'Color', [0.5 0.5 0.5], 'LineStyle', '--', 'LineWidth', 1);
text(22050,7, '22.05kHz')

% グラフタイトル、軸ラベル、凡例。
title('synthesizeNTF(osr=64,opt=1) F特 (44.1kHz PCM→2.8MHz 1bit SDM)');
xlabel('Frequency (Hz)');
ylabel('Gain (dB)');
legend('2nd order NTF', '3rd order NTF','4th order NTF', '5th order NTF', ...
       '6th order NTF', '7th order NTF', '8th order NTF', ...
       '9th order NTF', ...
       'Location', 'southeast');

% グラフの罫線を表示。
grid on

% y軸の表示範囲を-180dB～+20dBに設定する。
ylim([-180 20]);

