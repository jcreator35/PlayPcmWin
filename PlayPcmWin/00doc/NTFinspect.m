% uses delta sigma toolbox

figure('Name', '���[�v�t�B���^�[��NTF��F��', 'NumberTitle', 'off');
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
    
    f=logspace(log10(10/(originalSR*osr)), log10(0.5), 1000); % 10Hz����2.8MHz/2�܂őΐ����œ��Ԋu�̒l��1000�̃x�N�g���B
    z=exp(2i*pi*f);
    
    tf=evalTF(H, z);
    semilogx(f*originalSR*osr, dbv(tf));
    hold on % 1�̃O���t�Ƀv���b�g����B

    sigma_H = dbv(rmsGain(H, 0, 0.5/osr));
    fprintf('    sigma_H=%f\n', sigma_H);
end % order

% 22.05kHz�ɐ��������B
line([22050 22050], [20 -180], 'Color', [0.5 0.5 0.5], 'LineStyle', '--', 'LineWidth', 1);
text(22050,7, '22.05kHz')

% �O���t�^�C�g���A�����x���A�}��B
title('synthesizeNTF(osr=64,opt=1) F�� (44.1kHz PCM��2.8MHz 1bit SDM)');
xlabel('Frequency (Hz)');
ylabel('Gain (dB)');
legend('2nd order NTF', '3rd order NTF','4th order NTF', '5th order NTF', ...
       '6th order NTF', '7th order NTF', '8th order NTF', ...
       '9th order NTF', ...
       'Location', 'southeast');

% �O���t�̌r����\���B
grid on

% y���̕\���͈͂�-180dB�`+20dB�ɐݒ肷��B
ylim([-180 20]);

