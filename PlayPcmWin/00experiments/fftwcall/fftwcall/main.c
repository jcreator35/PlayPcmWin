#include <stdio.h>
#include <string.h>
#include <fftw3.h>
#include <math.h>

#define PI (3.14159265358979323846)

int main(void)
{
	double *in;
	fftw_complex *out;
	fftw_plan p;
	int i;
	int N = 256;
	size_t inSize = sizeof(double) * N;

	in = (double*) fftw_malloc(inSize);
	out = (fftw_complex*) fftw_malloc(sizeof(fftw_complex) * N);

	memset(in, 0, inSize);
	for (i=0; i<N; ++i) {
		double v = sin(PI * (double)i/N*4.0);
		in[i] = v;
	}

	for (i=0; i<N; ++i) {
		printf("%3d(%4.1f %4.1f) ", i, in[i], 0.0);
		if ((i%16) == 15) {
			printf("\n");
		}
	}
	printf("\n");

	p = fftw_plan_dft_r2c_1d(N, in, out, FFTW_ESTIMATE);

	fftw_execute(p); /* repeat as needed */

	for (i=0; i<N; ++i) {
		printf("%3d(%4.1f %4.1f) ", i, out[i][0], out[i][1]);
		if ((i%16) == 15) {
			printf("\n");
		}
	}

	fftw_destroy_plan(p);
	fftw_free(in);
	in = NULL;
	fftw_free(out);
	out = NULL;

	getchar();

	return 0;
}
