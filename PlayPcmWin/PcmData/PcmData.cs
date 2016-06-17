using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace PcmDataLib {
    /// <summary>
    /// PCMデータ情報置き場。
    /// ・PCMフォーマット情報
    ///   ・チャンネル数
    ///   ・サンプルレート
    ///   ・量子化ビット数
    ///   ・サンプルデータ形式(整数、浮動小数点数)
    /// ・PCMデータ
    ///   ・PCMデータ配列
    ///   ・PCMデータフレーム数(フレーム＝サンプル x チャンネル)
    /// ・ファイル管理情報
    ///   ・連番
    ///   ・ファイルグループ番号
    ///   ・ファイル名(ディレクトリ名を除く)
    ///   ・フルパスファイル名
    ///   ・表示名
    ///   ・開始Tick
    ///   ・終了Tick
    ///   ・トラック番号(CUEシート)
    /// </summary>
    public class PcmData {

        // PCMフォーマット情報 //////////////////////////////////////////////

        /// <summary>
        /// enum項目はWasapiCS.BitFormatTypeと同じ順番で並べる。
        /// </summary>
        public enum ValueRepresentationType {
            SInt,
            SFloat
        };

        /// <summary>
        /// チャンネル数
        /// </summary>
        public int NumChannels { get; set; }

        /// <summary>
        /// サンプルレート
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// 1サンプル値のビット数(無効な0埋めビット含む)
        /// </summary>
        public int BitsPerSample { get; set; }

        /// <summary>
        /// 1サンプル値の有効なビット数
        /// </summary>
        public int ValidBitsPerSample { get; set; }

        /// <summary>
        /// サンプル値形式(int、float)
        /// </summary>
        public ValueRepresentationType
            SampleValueRepresentationType { get; set; }


        public enum DataType {
            PCM,
            DoP
        };
        /// <summary>
        /// true: DoP false: PCM
        /// </summary>
        public DataType SampleDataType { get; set; }

        // PCMデータ ////////////////////////////////////////////////////////

        /// <summary>
        /// 総フレーム数(サンプル値の数÷チャンネル数)
        /// </summary>
        private long   mNumFrames;

        /// <summary>
        /// サンプル値配列。
        /// </summary>
        private byte[] mSampleArray;

        // ファイル管理情報 /////////////////////////////////////////////////

        /// <summary>
        /// 識別番号 0から始まる番号
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 連番(再生順) 0から始まる番号
        /// </summary>
        public int Ordinal { get; set; }

        /// <summary>
        /// ファイルグループ番号。 0から始まる番号
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// ファイル名(ディレクトリ名を除く)
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// フルパスファイル名
        /// </summary>
        public string FullPath { get; set; }

        /// <summary>
        /// 表示名。CUEシートから来る
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 開始Tick(75分の1秒=1)。0のとき、ファイルの先頭が開始Tick
        /// </summary>
        public int    StartTick { get; set; }

        /// <summary>
        /// 終了Tick(75分の1秒=1)。-1のとき、ファイルの終わりが終了Tick
        /// </summary>
        public int    EndTick { get; set; }

        /// <summary>
        /// アルバムタイトル
        /// </summary>
        public string AlbumTitle { get; set; }

        /// <summary>
        /// アーティスト
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// CUEシートから読んだ場合のINDEX番号(1==音声データ、0==無音)
        /// </summary>
        public int CueSheetIndex { get; set; }

        /// <summary>
        /// トラック番号(CUEシート)
        /// </summary>
        public int TrackId { get; set; }

        /// <summary>
        /// 画像バイト数
        /// </summary>
        public int PictureBytes { get; set; }

        /// <summary>
        /// 画像データバイト列
        /// </summary>
        public byte[] PictureData { get; set; }

        /// <summary>
        /// 長さ(秒)
        /// </summary>
        public int DurationSeconds {
            get {
                int seconds = (int)(NumFrames / SampleRate)
                        - StartTick / 75;
                if (0 <= EndTick) {
                    seconds = (EndTick - StartTick) / 75;
                }
                if (seconds < 0) {
                    seconds = 0;
                }
                return seconds;
            }
        }

        /// <summary>
        /// ファイルの最終書き込み時刻。
        /// </summary>
        public long LastWriteTime { get ;set; }

        /// <summary>
        /// rhsの内容をコピーする。PCMデータ配列だけはコピーしない。(nullをセットする)
        /// PCMデータ配列は、SetSampleArrayで別途設定する。
        /// </summary>
        /// <param name="rhs">コピー元</param>
        public void CopyHeaderInfoFrom(PcmData rhs) {
            NumChannels   = rhs.NumChannels;
            SampleRate    = rhs.SampleRate;
            BitsPerSample = rhs.BitsPerSample;
            ValidBitsPerSample = rhs.ValidBitsPerSample;
            SampleValueRepresentationType = rhs.SampleValueRepresentationType;
            mNumFrames = rhs.mNumFrames;
            mSampleArray = null;
            Id          = rhs.Id;
            Ordinal     = rhs.Ordinal;
            GroupId     = rhs.GroupId;
            FileName    = rhs.FileName;
            FullPath    = rhs.FullPath;
            DisplayName = rhs.DisplayName;
            StartTick   = rhs.StartTick;
            EndTick     = rhs.EndTick;
            AlbumTitle  = rhs.AlbumTitle;
            ArtistName   = rhs.ArtistName;
            CueSheetIndex = rhs.CueSheetIndex;
            PictureBytes = rhs.PictureBytes;
            PictureData = rhs.PictureData;
            SampleDataType = rhs.SampleDataType;
            LastWriteTime = rhs.LastWriteTime;
            TrackId = rhs.TrackId;
        }

        public PcmData() {
            NumChannels = 0;
            SampleRate = 0;
            BitsPerSample = 0;
            ValidBitsPerSample = 0;
            SampleValueRepresentationType = ValueRepresentationType.SInt;
            mNumFrames = 0;
            mSampleArray = null;
            Id = -1;
            Ordinal = -1;
            GroupId = -1;
            FileName = null;
            FullPath = null;
            DisplayName = null;
            StartTick = 0;
            EndTick = -1;
            AlbumTitle = "";
            ArtistName = "";
            CueSheetIndex = 1;
            PictureBytes = 0;
            PictureData = null;
            SampleDataType = DataType.PCM;
            LastWriteTime = -1;
            TrackId = 0;
        }


        /// <summary>
        /// ヘッダー情報、サンプルデータ領域をクローンする。
        /// </summary>
        public void CopyFrom(PcmData rhs) {
            CopyHeaderInfoFrom(rhs);

            mSampleArray = null;
            if (rhs.mSampleArray != null) {
                mSampleArray = (byte[])rhs.mSampleArray.Clone();
            }
        }

        // プロパティIO /////////////////////////////////////////////////////

        /// <summary>
        /// 総フレーム数(サンプル値の数÷チャンネル数)
        /// </summary>
        public long NumFrames {
            get { return mNumFrames; }
        }

        /// <summary>
        /// 1フレームあたりのビット数(サンプルあたりビット数×総チャンネル数)
        /// </summary>
        public int BitsPerFrame {
            get { return BitsPerSample * NumChannels; }
        }

        /// <summary>
        /// サンプル値配列
        /// </summary>
        public byte[] GetSampleArray() {
            return mSampleArray;
        }

        /// <summary>
        /// 総フレーム数(サンプル値の数÷チャンネル数)をセット
        /// </summary>
        public void SetNumFrames(long numFrames) {
            mNumFrames = numFrames;
        }

        /// <summary>
        /// サンプル配列を入れる。総フレーム数は別途セットする必要あり。
        /// </summary>
        /// <param name="sampleArray">サンプル配列</param>
        public void SetSampleArray(byte[] sampleArray) {
            mSampleArray = null;
            mSampleArray = sampleArray;
        }

        public void SetSampleArray(long numFrames, byte[] sampleArray) {
            mNumFrames = numFrames;
            mSampleArray = null;
            mSampleArray = sampleArray;
        }

        /// <summary>
        /// forget data part.
        /// PCMデータ配列を忘れる。
        /// サンプル数など、フォーマット情報は忘れない。
        /// </summary>
        public void ForgetDataPart() {
            mSampleArray = null;
        }

        public void SetPicture(int bytes, byte[] data) {
            PictureBytes = bytes;
            PictureData = data;
        }

        /// <summary>
        /// PCMデータの形式を設定する。
        /// </summary>
        public void SetFormat(
            int numChannels,
            int bitsPerSample,
            int validBitsPerSample,
            int sampleRate,
            ValueRepresentationType sampleValueRepresentation,
            long numFrames) {
            NumChannels = numChannels;
            BitsPerSample = bitsPerSample;
            ValidBitsPerSample = validBitsPerSample;
            SampleRate = sampleRate;
            SampleValueRepresentationType = sampleValueRepresentation;
            mNumFrames = numFrames;

            mSampleArray = null;
        }

        /// <summary>
        /// サンプリング周波数と量子化ビット数、有効なビット数、チャンネル数、データ形式が同じならtrue
        /// </summary>
        public bool IsSameFormat(PcmData other) {
            return BitsPerSample      == other.BitsPerSample
                && ValidBitsPerSample == other.ValidBitsPerSample
                && SampleRate    == other.SampleRate
                && NumChannels   == other.NumChannels
                && SampleValueRepresentationType == other.SampleValueRepresentationType
                && SampleDataType == other.SampleDataType;
        }

        /// <summary>
        /// StartTickとEndTickを見て、必要な部分以外をカットする。
        /// </summary>
        public void Trim() {
            if (StartTick < 0) {
                // データ壊れ。先頭を読む。
                StartTick = 0;
            }

            long startFrame = (long)(StartTick) * SampleRate / 75;
            long endFrame   = (long)(EndTick)   * SampleRate / 75;

            TrimInternal(startFrame, endFrame);
        }

        /// <summary>
        /// startFrameからendFrameまでの範囲にする。
        /// </summary>
        /// <param name="startFrame">0: 先頭 </param>
        /// <param name="endFrame">負: 最後まで。0以上: 範囲外の最初のデータoffset。 0の場合0サンプルとなる</param>
        public void TrimByFrame(long startFrame, long endFrame) {
            TrimInternal(startFrame, endFrame);
        }

        private void TrimInternal(long startFrame, long endFrame) {
            if (startFrame == 0 && endFrame < 0) {
                // データTrimの必要はない。
                return;
            }

            if (endFrame < 0 ||
                NumFrames < endFrame) {
                // 終了位置はファイルの終わり。
                endFrame = NumFrames;
            }

            if (endFrame < startFrame) {
                // 1サンプルもない。
                startFrame = endFrame;
            }

            long startBytes = startFrame * BitsPerFrame / 8;
            long endBytes   = endFrame   * BitsPerFrame / 8;

            Debug.Assert(0 <= startBytes);
            Debug.Assert(0 <= endBytes);
            Debug.Assert(startBytes <= endBytes);
            Debug.Assert(null != mSampleArray);
            Debug.Assert(startBytes <= mSampleArray.Length);
            Debug.Assert(endBytes <= mSampleArray.Length);

            long newNumSamples = endFrame - startFrame;
            mNumFrames = newNumSamples;
            if (newNumSamples == 0 ||
                mSampleArray.Length <= startBytes) {
                mSampleArray = null;
                mNumFrames = 0;
            } else {
                byte[] newArray = new byte[endBytes - startBytes];
                Array.Copy(mSampleArray, startBytes, newArray, 0, endBytes - startBytes);
                mSampleArray = null;
                mSampleArray = newArray;
            }
        }

        /// <summary>
        /// サンプル値取得。フォーマットがなんであっても使用可能。
        /// </summary>
        /// <param name="ch">チャンネル番号</param>
        /// <param name="pos">サンプル番号</param>
        /// <returns>サンプル値。-1.0～+1.0位</returns>
        public double GetSampleValueInDouble(int ch, long pos) {
            Debug.Assert(0 <= ch && ch < NumChannels);

            if (pos < 0 || NumFrames <= pos) {
                return 0.0;
            }

            long offset = pos * BitsPerFrame/8 + ch * BitsPerSample/8;
            Debug.Assert(offset <= 0x7fffffffL);

            byte [] data = new byte[BitsPerSample / 8];
            Array.Copy(mSampleArray, offset, data, 0, BitsPerSample / 8);

            switch (BitsPerSample) {
            case 16:
                data = ConvI16toF64(data);
                break;
            case 24:
                data = ConvI24toF64(data);
                break;
            case 32:
                switch (SampleValueRepresentationType) {
                case ValueRepresentationType.SInt:
                    data = ConvI32toF64(data);
                    break;
                case ValueRepresentationType.SFloat:
                    data = ConvF32toF64(data);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
                break;
            case 64:
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            return BitConverter.ToDouble(data, 0);
        }

        private byte[] ConvI16toF64(byte[] from) {
            int nSample = from.Length / 2;
            byte[] to = new byte[nSample * 8];
            int fromPos = 0;
            int toPos = 0;
            for (int i = 0; i < nSample; ++i) {
                short iv = (short)(from[fromPos]
                    + (from[fromPos + 1] << 8));
                double dv = ((double)iv) * (1.0 / 32768.0);

                byte [] b = System.BitConverter.GetBytes(dv);

                for (int j=0; j < 8; ++j) {
                    to[toPos++] = b[j];
                }
                fromPos += 2;
            }
            return to;
        }

        private byte[] ConvI24toF64(byte[] from) {
            int nSample = from.Length / 3;
            byte[] to = new byte[nSample * 8];
            int fromPos = 0;
            int toPos = 0;
            for (int i = 0; i < nSample; ++i) {
                int iv = ((int)from[fromPos] << 8)
                    + ((int)from[fromPos + 1] << 16)
                    + ((int)from[fromPos + 2] << 24);
                double dv = ((double)iv) * (1.0 / 2147483648.0);

                byte [] b = System.BitConverter.GetBytes(dv);

                for (int j=0; j < 8; ++j) {
                    to[toPos++] = b[j];
                }
                fromPos += 3;
            }
            return to;
        }

        private byte[] ConvI32toF64(byte[] from) {
            int nSample = from.Length / 4;
            byte[] to = new byte[nSample * 8];
            int fromPos = 0;
            int toPos = 0;
            for (int i = 0; i < nSample; ++i) {
                int iv = ((int)from[fromPos + 1] << 8)
                    + ((int)from[fromPos + 2] << 16)
                    + ((int)from[fromPos + 3] << 24);
                double dv = ((double)iv) * (1.0 / 2147483648.0);

                byte [] b = System.BitConverter.GetBytes(dv);

                for (int j=0; j < 8; ++j) {
                    to[toPos++] = b[j];
                }
                fromPos += 4;
            }
            return to;
        }

        private byte[] ConvF32toF64(byte[] from) {
            int nSample = from.Length / 4;
            byte[] to = new byte[nSample * 8];
            int fromPos = 0;
            int toPos = 0;
            for (int i = 0; i < nSample; ++i) {
                float fv = System.BitConverter.ToSingle(from, fromPos);
                double dv = (double)fv;

                byte [] b = System.BitConverter.GetBytes(dv);
                for (int j=0; j < 8; ++j) {
                    to[toPos++] = b[j];
                }
                fromPos += 4;
            }
            return to;
        }

        /// <summary>
        /// double サンプル値セット。フォーマットが64bit SFloatの場合のみ使用可能。
        /// </summary>
        public void SetSampleValueInDouble(int ch, long pos, double val) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 64);
            Debug.Assert(0 <= ch && ch < NumChannels);

            if (pos < 0 || NumFrames <= pos) {
                return;
            }

            long offset = pos * BitsPerFrame / 8 + ch * BitsPerSample / 8;
            Debug.Assert(offset <= 0x7fffffffL);

            var byteArray = BitConverter.GetBytes(val);
            Buffer.BlockCopy(byteArray, 0, mSampleArray, (int)offset, 8);
        }

        /// <summary>
        /// float サンプル値取得。フォーマットが32bit floatの場合のみ使用可能。
        /// </summary>
        /// <param name="ch">チャンネル番号</param>
        /// <param name="pos">サンプル番号</param>
        /// <returns>サンプル値。-1.0～+1.0位</returns>
        public float GetSampleValueInFloat(int ch, long pos) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 32);
            Debug.Assert(0 <= ch && ch < NumChannels);

            if (pos < 0 || NumFrames <= pos) {
                return 0.0f;
            }

            long offset = pos * BitsPerFrame / 8 + ch * BitsPerSample / 8;
            Debug.Assert(offset <= 0x7fffffffL);

            return BitConverter.ToSingle(mSampleArray, (int)offset);
        }

        /// <summary>
        /// サンプル値セット。フォーマットが32bit SFloatの場合のみ使用可能。
        /// </summary>
        public void SetSampleValueInFloat(int ch, long pos, float val) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 32);
            Debug.Assert(0 <= ch && ch < NumChannels);

            if (pos < 0 || NumFrames <= pos) {
                return;
            }

            long offset = pos * BitsPerFrame / 8 + ch * BitsPerSample / 8;
            Debug.Assert(offset <= 0x7fffffffL);

            var byteArray = BitConverter.GetBytes(val);
            Buffer.BlockCopy(byteArray, 0, mSampleArray, (int)offset, 4);
        }

        public int GetSampleValueInInt32(int ch, long pos) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SInt);
            Debug.Assert(0 <= ch && ch < NumChannels);
            if (pos < 0 || NumFrames <= pos) {
                return 0;
            }

            long offset = pos * BitsPerFrame / 8 + ch * BitsPerSample / 8;
            Debug.Assert(offset <= 0x7fffffffL);

            switch (BitsPerSample) {
            case 16:
                return (mSampleArray[offset] << 16) + (mSampleArray[offset+1] << 24);
            case 24:
                return (mSampleArray[offset] << 8) + (mSampleArray[offset + 1] << 16) + (mSampleArray[offset + 2] << 24);
            case 32:
                switch (ValidBitsPerSample) {
                case 24:
                    return (mSampleArray[offset + 1] << 8) + (mSampleArray[offset + 2] << 16) + (mSampleArray[offset + 3] << 24);
                case 32:
                    return (mSampleArray[offset]) + (mSampleArray[offset + 1] << 8) + (mSampleArray[offset + 2] << 16) + (mSampleArray[offset + 3] << 24);
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return 0;
                }
            default:
                System.Diagnostics.Debug.Assert(false);
                return 0;
            }
        }

        /// <summary>
        /// サンプル値セット。フォーマットがSintの場合のみ使用可能。
        /// </summary>
        public void SetSampleValueInInt32(int ch, long pos, int val) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SInt);
            Debug.Assert(0 <= ch && ch < NumChannels);
            if (pos < 0 || NumFrames <= pos) {
                return;
            }

            long offset = pos * BitsPerFrame / 8 + ch * BitsPerSample / 8;
            Debug.Assert(offset <= 0x7fffffffL);

            switch (BitsPerSample) {
            case 16:
                mSampleArray[offset + 0] = (byte)(0xff & (val >> 16));
                mSampleArray[offset + 1] = (byte)(0xff & (val >> 24));
                return;
            case 24:
                mSampleArray[offset + 0] = (byte)(0xff & (val >> 8));
                mSampleArray[offset + 1] = (byte)(0xff & (val >> 16));
                mSampleArray[offset + 2] = (byte)(0xff & (val >> 24));
                return;
            case 32:
                switch (ValidBitsPerSample) {
                case 24:
                    mSampleArray[offset + 0] = 0;
                    mSampleArray[offset + 1] = (byte)(0xff & (val >> 8));
                    mSampleArray[offset + 2] = (byte)(0xff & (val >> 16));
                    mSampleArray[offset + 3] = (byte)(0xff & (val >> 24));
                    return;
                case 32:
                    mSampleArray[offset + 0] = (byte)(0xff & (val));
                    mSampleArray[offset + 1] = (byte)(0xff & (val >> 8));
                    mSampleArray[offset + 2] = (byte)(0xff & (val >> 16));
                    mSampleArray[offset + 3] = (byte)(0xff & (val >> 24));
                    return;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    return;
                }
            default:
                System.Diagnostics.Debug.Assert(false);
                return;
            }
        }

        /// <summary>
        /// doubleのバッファをスケーリングする。ダブルバッファとは関係ない
        /// </summary>
        public void ScaleDoubleBuffer(double scale) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 64);
            for (int i = 0; i < NumFrames * NumChannels; ++i) {
                double v = BitConverter.ToDouble(mSampleArray, i * 8);
                v *= scale;
                var byteArray = BitConverter.GetBytes(v);
                Buffer.BlockCopy(byteArray, 0, mSampleArray, i * 8, 8);
            }
        }

        /// <summary>
        /// floatのバッファをスケーリングする。
        /// </summary>
        public void ScaleFloatBuffer(float scale) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 32);
            for (int i = 0; i < NumFrames * NumChannels; ++i) {
                float v = BitConverter.ToSingle(mSampleArray, i * 4);
                v *= scale;
                var byteArray = BitConverter.GetBytes(v);
                Buffer.BlockCopy(byteArray, 0, mSampleArray, i * 4, 4);
            }
        }

        /// <summary>
        /// doubleのバッファで最大値、最小値を取得
        /// </summary>
        public void FindMaxMinValueOnDoubleBuffer(out double maxV, out double minV) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 64);
            maxV = 0.0;
            minV = 0.0;

            for (int i = 0; i < NumFrames * NumChannels; ++i) {
                double v = BitConverter.ToDouble(mSampleArray, i * 8);
                if (v < minV) {
                    minV = v;
                }
                if (maxV < v) {
                    maxV = v;
                }
            }
        }

        private static readonly double SAMPLE_VALUE_MAX_DOUBLE =  1.0;
        private static readonly double SAMPLE_VALUE_MIN_DOUBLE = -1.0;
        private static readonly float  SAMPLE_VALUE_MAX_FLOAT  =  1.0f;
        private static readonly float  SAMPLE_VALUE_MIN_FLOAT  = -1.0f;

        /// <summary>
        /// floatのバッファで最大値、最小値を取得
        /// </summary>
        public void FindMaxMinValueOnFloatBuffer(out float maxV, out float minV) {
            Debug.Assert(SampleValueRepresentationType == ValueRepresentationType.SFloat);
            Debug.Assert(ValidBitsPerSample == 32);
            maxV = 0.0f;
            minV = 0.0f;

            for (int i = 0; i < NumFrames * NumChannels; ++i) {
                float v = BitConverter.ToSingle(mSampleArray, i * 4);
                if (v < minV) {
                    minV = v;
                }
                if (maxV < v) {
                    maxV = v;
                }
            }
        }

        /// <summary>
        /// doubleのバッファで音量制限する。
        /// </summary>
        /// <returns>スケーリングが行われた場合、スケールの倍数(1.0より小さい)。行われなかった場合1.0</returns>
        public double LimitLevelOnDoubleRange() {
            double maxV;
            double minV;
            FindMaxMinValueOnDoubleBuffer(out maxV, out minV);

            double scale = 1.0;
            if (SAMPLE_VALUE_MAX_DOUBLE < maxV) {
                scale = SAMPLE_VALUE_MAX_DOUBLE / maxV;
            }
            if (minV < SAMPLE_VALUE_MIN_DOUBLE && SAMPLE_VALUE_MIN_DOUBLE / minV < scale) {
                scale = SAMPLE_VALUE_MIN_DOUBLE / minV;
            }

            if (scale < 1.0) {
                ScaleDoubleBuffer(scale);
            }

            return scale;
        }

        /// <summary>
        /// floatのバッファで音量制限する。
        /// </summary>
        /// <returns>スケーリングが行われた場合、スケールの倍数。行われなかった場合1.0f</returns>
        public float LimitLevelOnFloatRange() {
            float maxV;
            float minV;
            FindMaxMinValueOnFloatBuffer(out maxV, out minV);

            float scale = 1.0f;
            if (SAMPLE_VALUE_MAX_FLOAT < maxV) {
                scale = SAMPLE_VALUE_MAX_FLOAT / maxV;
            }
            if (minV < SAMPLE_VALUE_MIN_FLOAT && SAMPLE_VALUE_MIN_FLOAT / minV < scale) {
                scale = SAMPLE_VALUE_MIN_FLOAT / minV;
            }

            if (scale < 1.0f) {
                ScaleFloatBuffer(scale);
            }

            return scale;
        }

        public PcmData ConvertChannelCount(int newCh) {
            if (NumChannels == newCh) {
                // 既に希望のチャンネル数である。
                return this;
            }

            // サンプルあたりビット数が8の倍数でないとこのアルゴリズムは使えない
            System.Diagnostics.Debug.Assert((BitsPerSample & 7) == 0);

            // 新しいサンプルサイズ
            // NumFramesは総フレーム数。sampleArrayのフレーム数はこれよりも少ないことがある。
            // 実際に存在するサンプル数sampleFramesだけ処理する。
            int bytesPerSample = BitsPerSample / 8;
            long sampleFrames = mSampleArray.LongLength / (BitsPerFrame / 8);
            var newSampleArray = new byte[newCh * bytesPerSample * sampleFrames];

            for (long frame = 0; frame < sampleFrames; ++frame) {
                int copyBytes = NumChannels * bytesPerSample;
                if (newCh < NumChannels) {
                    // チャンネル数が減る場合。
                    copyBytes = newCh * bytesPerSample;
                }

                Array.Copy(mSampleArray, NumChannels * bytesPerSample * frame,
                    newSampleArray, newCh * bytesPerSample * frame,
                    copyBytes);
                if (SampleDataType == DataType.DoP
                        && NumChannels < newCh) {
                    // 追加したチャンネルにDSD無音をセットする。
                    switch (bytesPerSample) {
                    case 3:
                        for (int ch = NumChannels; ch < newCh; ++ch) {
                            newSampleArray[(frame * newCh + ch) * bytesPerSample + 0] = 0x69;
                            newSampleArray[(frame * newCh + ch) * bytesPerSample + 1] = 0x69;
                            newSampleArray[(frame * newCh + ch) * bytesPerSample + 2] = (byte)((frame & 1) == 1 ? 0xfa : 0x05);
                        }
                        break;
                    case 4:
                        for (int ch = NumChannels; ch < newCh; ++ch) {
                            newSampleArray[(frame * newCh + ch) * bytesPerSample + 1] = 0x69;
                            newSampleArray[(frame * newCh + ch) * bytesPerSample + 2] = 0x69;
                            newSampleArray[(frame * newCh + ch) * bytesPerSample + 3] = (byte)((frame & 1) == 1 ? 0xfa : 0x05);
                        }
                        break;
                    }
                }
            }

            PcmData newPcmData = new PcmData();
            newPcmData.CopyHeaderInfoFrom(this);
            newPcmData.SetFormat(newCh, BitsPerSample, ValidBitsPerSample, SampleRate, SampleValueRepresentationType, NumFrames);
            newPcmData.SetSampleArray(newSampleArray);

            return newPcmData;
        }

        public PcmData AddSilentForEvenChannel() {
            if ((NumChannels & 1) == 0) {
                // 既にチャンネル数が偶数。
                return this;
            }

            return ConvertChannelCount(NumChannels + 1);
        }

        public PcmData MonoToStereo() {
            System.Diagnostics.Debug.Assert(NumChannels == 1);

            // サンプルあたりビット数が8の倍数でないとこのアルゴリズムは使えない
            System.Diagnostics.Debug.Assert((BitsPerSample & 7) == 0);

            byte [] newSampleArray = new byte[mSampleArray.LongLength * 2];

            {
                int bytesPerSample = BitsPerSample / 8;

                // sampleArrayのフレーム数はこれよりも少ないことがある。
                // 実際に存在するサンプル数sampleFramesだけ処理する。
                long sampleFrames = mSampleArray.LongLength / bytesPerSample; // NumChannels==1なので。
                long fromPosBytes = 0;
                for (long frame = 0; frame < sampleFrames; ++frame) {
                    for (int offs = 0; offs < bytesPerSample; ++offs) {
                        newSampleArray[fromPosBytes * 2 + offs] = mSampleArray[fromPosBytes + offs];
                        newSampleArray[fromPosBytes * 2 + bytesPerSample + offs] = mSampleArray[fromPosBytes + offs];
                    }
                    fromPosBytes += bytesPerSample;
                }
            }
            PcmData newPcmData = new PcmData();
            newPcmData.CopyHeaderInfoFrom(this);
            newPcmData.SetFormat(2, BitsPerSample, ValidBitsPerSample, SampleRate, SampleValueRepresentationType, NumFrames);
            newPcmData.SetSampleArray(newSampleArray);

            return newPcmData;
        }
    }
}
