SAMPLE_RATE   = 44100
WINDOW_LENGTH = 16384
TEMPORAL_WIDTH = 8

import csv
import tensorflow as tf
from tensorflow import keras
import numpy as np
import matplotlib.pyplot as plt
from extractFreq import extractFreq

# key fftbin test
#s = extractFreq("HarpsichordScale.wav", SAMPLE_RATE, WINDOW_LENGTH, 0, 87, "ChromaticScale")
#plt.figure(figsize=(s.shape[0]/10,s.shape[1]/10))
#plt.imshow(np.transpose(s),cmap=plt.get_cmap('binary'))
#plt.grid(False)
#plt.title("ChromaticScale44100")
#plt.savefig("out/ChromaticScale44100.png")

#print(tf.__version__)

import pandas as pd

def CreateKeyIdx(keys):
    idx = 0
    keyToIdx = {}
    idxToKey = {}
    for k in keys:
        if k in keyToIdx:
            pass
        else:
            keyToIdx[k] = idx
            idxToKey[idx] = k
            idx = idx + 1
    return (keyToIdx, idxToKey)

spectra = {}
keys = []

key_spectra = {}

def Process1(keysPath, wavDir, spectra, keys):
    print("Process1({0}, {1})".format(keysPath, wavDir))
    df=pd.read_csv(keysPath,usecols = [0,1,2,3], skiprows = [0],header=None)
    d = df.values
    #print(d)
    #print(len(d))
    for v in d:
        Knr = v[0]
        startSec = v[1]
        endSec=v[2]
        key=v[3]
        path = "{0}/K{1}.wav".format(wavDir, Knr)
        #print("K{0}.wav startSec={1} endSec={2}".format(Knr, startSec, endSec))
        s = extractFreq(path,SAMPLE_RATE, WINDOW_LENGTH, startSec,endSec,key)

        #print(s.shape[0])
        a = [] 
        for i in range(s.shape[0]//TEMPORAL_WIDTH):
            a = np.append(s[i*TEMPORAL_WIDTH:(i+1)*TEMPORAL_WIDTH,:], a)
        #print(a.shape)
        #print(a.ndim) # == 1
        
        if key in spectra:
            prev = spectra[key]
            #print(prev.shape)
            #print(s.shape)
            spectra[key] = np.append(prev, s, axis=0)
            key_spectra[key] = np.append(key_spectra[key], a)
            #print(key_spectra[key].shape)
            #print(key_spectra[key].ndim) # == 1
        else:
            spectra[key] = s
            keys.append(key)
            key_spectra[key] = a

Process1("KeysRef.csv", "WAV_Reference", spectra, keys)
Process1("KeysL1.csv", "WAV_L1", spectra, keys)
Process1("KeysL2.csv", "WAV_L2", spectra, keys)

(keyToIdx, idxToKey) = CreateKeyIdx(keys)

#print(keyToIdx)
#print(idxToKey)
#print(keys)

fields = ['Key']
for c in ['S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z']: #['']:# ['X', 'Y', 'Z', 'W']:
    for p in ['A2', 'B2', 'H2', 'C3', 'Cis3', 'D3', 'Dis3', 'E3', 'F3', 'Fis3', 'G3', 'Gis3', \
            'A3', 'B3', 'H3', 'C4', 'Cis4', 'D4', 'Dis4', 'E4', 'F4', 'Fis4', 'G4', 'Gis4', \
            'A4', 'B4', 'H4', 'C5', 'Cis5', 'D5', 'Dis5', 'E5', 'F5', 'Fis5', 'G5', 'Gis5', \
            'A5', 'B5', 'H5', 'C6', 'Cis6', 'D6', 'Dis6', 'E6' ]:
        fields.append("{0}{1}".format(c,p))
#print(fields)

fn = "spectraTable_{0}.csv".format(TEMPORAL_WIDTH)
with open(fn, 'w', newline='') as csvfile:
    cw = csv.writer(csvfile, lineterminator="\n")
    cw.writerow(fields)

    for key in keys:
        s = spectra[key]
        #print(s.shape)
        #plt.figure(figsize=(s.shape[0]/10,s.shape[1]/10))
        #plt.imshow(np.transpose(s), cmap=plt.get_cmap('binary'))
        #plt.grid(False)
        #plt.title(key)
        plt.savefig("out/{0}.png".format(key))

        ks = key_spectra[key]
        kscount = ks.shape[0] // (TEMPORAL_WIDTH * 44)
        ks2 = ks.reshape((kscount, TEMPORAL_WIDTH*44))
        #print(ks2.shape)
        for i in range(kscount):
            r = [key]
            r.extend(ks2[i,:].tolist())
            cw.writerow(r)

print("Completed.")
