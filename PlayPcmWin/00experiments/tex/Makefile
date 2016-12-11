all: ButterworthLowpassFilter.pdf LaplaceTransform.pdf NoiseShaping.pdf

clean:
	rm -f *.dvi *.pdf *.aux *.log 

%.pdf : %.dvi
	dvipdfmx $<

%.dvi : %.tex
	platex $<

