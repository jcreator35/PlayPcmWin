% Plots the frequency spectrum of 2.8224MHz PCM 1.000kHz sine signal,
%   At least 4 minutes of continuous tone is needed. 

% 1kHz signal takes 2822.4 samples.
% 1kHz signal x 5 period takes 2822.4 * 5 = 14112 samples.
FFT_LEN             = 14112 * 32;
FREQUENCY_AVG_COUNT = 32;
TIME_AVG_COUNT      = 32;

% 1248 == delay samples of the filter now testing: first zero cross sample pos of sine signal.
START_DISCARD_SAMPLES = 1248;

PlotSpectrumAveraged('output_woDither.wav',     'Without dither',        FFT_LEN, START_DISCARD_SAMPLES, TIME_AVG_COUNT, FREQUENCY_AVG_COUNT);
hold on;
PlotSpectrumAveraged('output_0_0625Dither.wav', 'With 1/16 TPDF dither', FFT_LEN, START_DISCARD_SAMPLES, TIME_AVG_COUNT, FREQUENCY_AVG_COUNT);
PlotSpectrumAveraged('output_0_125Dither.wav',  'With 1/8 TPDF dither',  FFT_LEN, START_DISCARD_SAMPLES, TIME_AVG_COUNT, FREQUENCY_AVG_COUNT);
PlotSpectrumAveraged('output_0_25Dither.wav',   'With 1/4 TPDF dither',  FFT_LEN, START_DISCARD_SAMPLES, TIME_AVG_COUNT, FREQUENCY_AVG_COUNT);

title('Dither comparison');
legend('Location','southeast');
set(gcf, 'color', [1 1 1]);
set(gcf, 'position', [0, 0, 800, 600]);
set(gca, 'XMinorTick', 'on');
set(gca, 'YMinorTick', 'on');
grid on
grid minor
    