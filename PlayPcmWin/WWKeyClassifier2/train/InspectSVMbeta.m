% MatlabのclassificationLearner()で学習。LinearSVMのtrained modelを出力。
load('trainedModel8.mat');

cn = trainedModel8.ClassificationSVM.ClassNames;

% 44個の鍵盤。
% 8つの時間のサンプル値。

c = 1;
for i=1:23
    for j=i+1:24
        beta = trainedModel8.ClassificationSVM.BinaryLearners{c,1}.Beta;
        beta8 = reshape(beta,[44,8]);
        %image(beta8, 'CDataMapping', 'scaled');colorbar;
        % 時間軸方向に平均。44個のデータを得る。
        beta8m = mean(beta8,2);

        SaveGraph(beta8m, cn(i), cn(j), c);
        c = c + 1;
    end
end

function SaveGraph(x,keyA, keyB, c)
    keys=categorical({'A2','B2','H2','C3','Cis3','D3','Dis3','E3','F3','Fis3','G3','Gis3','A3','B3','H3','C4','Cis4','D4','Dis4','E4','F4','Fis4','G4','Gis4','A4','B4','H4','C5','Cis5','D5','Dis5','E5','F5','Fis5','G5','Gis5','A5','B5','H5','C6','Cis6','D6','Dis6','E6'});
    keys=reordercats(keys,{'A2','B2','H2','C3','Cis3','D3','Dis3','E3','F3','Fis3','G3','Gis3','A3','B3','H3','C4','Cis4','D4','Dis4','E4','F4','Fis4','G4','Gis4','A4','B4','H4','C5','Cis5','D5','Dis5','E5','F5','Fis5','G5','Gis5','A5','B5','H5','C6','Cis6','D6','Dis6','E6'});
    figure('Visible','off')
    set(gcf,'Visible','off','CreateFcn','set(gcf,''Visible'',''on'')')
    barh(keys,x);
    xlim([-1.5 1.5])
    labelStr = sprintf('←%s   %s→', keyB, keyA);
    xlabel(labelStr);
    titleStr = sprintf('Difference from %s and %s', keyA, keyB);
    title(titleStr);
    grid on
    set(gcf,'position',[10,100,512,800])
    outPath=sprintf('graph\\%d_%s_%s.png', c, keyA, keyB);
    saveas(gcf, outPath);
end



