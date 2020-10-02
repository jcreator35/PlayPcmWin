import tensorflow as tf
from tensorflow import keras
import numpy as np
import matplotlib.pyplot as plt
from scipy.io import wavfile as wav

def extractFreq(path, SAMPLE_RATE, WINDOW_LENGTH, startSec, endSec, key):
    fIdx=[]
    f = 110 # Hz
    while f < 1320:
        fIdx.append((int)(f * ((WINDOW_LENGTH/2)/(SAMPLE_RATE/2))))
        f *= pow(2,1.0/12)
    #fIdx = np.array(fIdx)
    print(fIdx)
    #print(fIdx.shape)
    #print(fIdx.dtype)

    sampling_rate, tdata =  wav.read(path)
    if (sampling_rate != SAMPLE_RATE):
        sys.exit("sampling rate != {0}".format(SAMPLE_RATE))
    if tdata.ndim > 1:
        # get first channel
        tdata = tdata[:,0]
    tdataF = tf.cast(tdata, tf.float32)
    tdataF = tf.nn.batch_normalization(tdataF, 0, 1, None, None, tf.keras.backend.epsilon())

    #print(tdataF.dtype)
    #print(sampling_rate)
    #print(tdataF.shape)
    
    if np.size(tdata) <= endSec * SAMPLE_RATE:
        endSec = np.size(tdata) // SAMPLE_RATE

    start4K = WINDOW_LENGTH*((int)(startSec * SAMPLE_RATE) // WINDOW_LENGTH)
    end4K   = WINDOW_LENGTH*((int)(endSec * SAMPLE_RATE) // WINDOW_LENGTH)

    #print(start4K)
    #print(end4K)

    tdataFs = tf.slice(tdataF, [start4K], [end4K-start4K])
    tdataF2 = tf.reshape(tdataFs, [-1, WINDOW_LENGTH])

    #print(tdataF2.shape)

    w = tf.signal.hann_window(WINDOW_LENGTH)

    #fdataN = tf.zeros((tdataF2.shape[0],4096))
    #r = tf.signal.rfft(fdataN)
    tdataW = tdataF2 * w
    
    # rfft means real valued fft
    fdataN = abs(tf.signal.rfft(tdataW))

    #print(fdataN.shape)
    #print(fdataN.dtype)

    fdataN = np.array(fdataN)

    #pick up frequency component
    fdataK = fdataN[:,fIdx]
    #print(fdataK.shape)


    #plt.figure()
    #plt.imshow(np.transpose(fdataK))
    #plt.grid(False)
    #plt.title("K{0}: {1}".format(Knr,key))
    #plt.show()
    return fdataK
