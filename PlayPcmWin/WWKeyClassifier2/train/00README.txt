■ Preparing WAV data folder WAV_Reference, WAV_L1 and WAV_L2

Rip Scarlatti Scott Ross CDs from K1 to K100 to create K1.flac to K100.flac

Original flac files convert to monaural WAV and store them on WAV_L1 directory.

Increase pitch by semitone (it becomes to concert pitch) using WWArbitraryResampler. And convert them to monaural WAV using convFlacToWav.sh and store them on WAV_Reference directory.

Decrease pitch by semitone and convert to WAV and store them on WAV_L2 directory.

■ Creating KeysRef.csv, KeysL1.csv and KeysL2.csv

Listen to concert pitch files from K1 to K100 and roughly write down time and key: Keys_faithful.csv

On K25, Cisdur appears but it is changed to Desdur to reduce the number of keys 24: KeysRef.csv

Convert it to semitone lower pitch data KeysL1.csv using KeyListConv.cs

Convert it to wholetone lower pitch data KeysL2.csv using KeyListConv.cs

■ Creating spectre data for learning

Run KeysCsvToSpectraTable.py to create spectraTable_8.csv

■ Create trained model using Matlab

Open Matlab, run classificationLearner() and input spectraTable_8.csv and learn LinearSVM to create linear SVM model and export as trainedModel8.mat

■ Create Linear SVM coefficient table for C sharp

Run ExportClassificationSVMtoCsharp.m on Matlab to create C sharp code snippet to feed to WWMath/MulticlassLinearSVMClassifier.cs and WWMath/BinarySVMClassifier.cs

Paste resulted code snippet to WWKeyClassifier2/KeyClassifierCore.cs:139

Uncomment KeyClassifier.cs ctor test code and instancing to test classifier.

