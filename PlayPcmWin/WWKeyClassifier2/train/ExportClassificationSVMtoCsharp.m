% 割り切れる数に設定する。
PRINT_STRIDE = 44;

% t contains classificationSVM object
t = load("trainedModel8.mat");

yPred = t.trainedModel8.predictFcn(t.trainedModel8.ClassificationSVM.X);
confusionchart(t.trainedModel8.ClassificationSVM.Y,yPred, 'Normalization', 'row-normalized', 'RowSummary', 'row-normalized')
title("LinearSVM with cross varidation=10")

fileID = fopen('ClassifyTableSnippet.cs', 'w');

% input data names ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
pnsz = size(t.trainedModel8.ClassificationSVM.PredictorNames);
% [1,352]

fprintf(fileID, "// Exported using ExportClassificationsSVM.m\n");
fprintf(fileID, "\n");

fprintf(fileID, "private string [] mInputPredictorNames = { ");
for i=1:pnsz(2)
    fprintf(fileID, """%s"", ", t.trainedModel8.ClassificationSVM.PredictorNames{i});
end
fprintf(fileID, "};\n");

% output labels ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
cnsz = size(t.trainedModel8.ClassificationSVM.ClassNames);
% [24,1]
fprintf(fileID, "private string [] mOutputLabelNames = { ");
for i=1:cnsz(1)
    fprintf(fileID, """%s"", ", t.trainedModel8.ClassificationSVM.ClassNames(i));
end
fprintf(fileID, "};\n");

% BinaryLearner params ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
fprintf(fileID, "// yPred = vectorDot((X/scale), beta) + bias\n");
fprintf(fileID, "struct BinaryLinearSVMParams {\n");
fprintf(fileID, "    public float scale;\n");
fprintf(fileID, "    public float bias;\n");
fprintf(fileID, "    public float [] beta;\n");
fprintf(fileID, "    public BinaryLinearSVMParams(float aScale, float aBias, float [] aBeta) {\n");
fprintf(fileID, "        scale = aScale;\n");
fprintf(fileID, "        bias=aBias;\n");
fprintf(fileID, "        beta=aBeta;\n");
fprintf(fileID, "    }\n");
fprintf(fileID, "}\n");
fprintf(fileID, "\n");

blsz = size(t.trainedModel8.ClassificationSVM.BinaryLearners);
% [276,1]

fprintf(fileID, "private BinaryLinearSVMParams[] mBinaryLinearSVMParamAry = new BinaryLinearSVMParams[] {\n");
for i=1:blsz(1)
    bl = t.trainedModel8.ClassificationSVM.BinaryLearners{i,1};
    fprintf(fileID, "    new BinaryLinearSVMParams(%ff, %ff, new float[] {", bl.KernelParameters.Scale, bl.Bias);
    for j=1:PRINT_STRIDE:pnsz(2)
        fprintf(fileID, "%ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, ", bl.Beta(j:(j+PRINT_STRIDE-1)));
    end
    fprintf(fileID, "}),\n");
end
fprintf(fileID, "};\n");
fprintf(fileID, "\n");


% test data set ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

tbsz = size(t.trainedModel8.ClassificationSVM.X);
% [3828,352]

fprintf(fileID, "public struct TrainData {\n");
fprintf(fileID, "    public float [] x;\n");
fprintf(fileID, "    public int y;\n");
fprintf(fileID, "    public TrainData(float [] aX, int aY) {\n");
fprintf(fileID, "        x = aX;\n");
fprintf(fileID, "        y = aY;\n");
fprintf(fileID, "    }\n");
fprintf(fileID, "};\n");

S = vartype('numeric');
fprintf(fileID, "public TrainData [] TrainDataAry = new TrainData[] {\n");
for i=1:tbsz(1)
    fprintf(fileID, "    new TrainData(new float[] { \n");
    
    for j=1:PRINT_STRIDE:pnsz(2)
        fprintf(fileID, "%ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff,\n %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff, %ff,\n", t.trainedModel8.ClassificationSVM.X{i,j:(j+PRINT_STRIDE-1)});
    end
    % grp2idx starts from 1 so decrement it.
    fprintf(fileID, "}, %d),\n", grp2idx(t.trainedModel8.ClassificationSVM.Y(i)) -1);
end
fprintf(fileID, "};\n");

fclose(fileID);

