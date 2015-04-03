#include <windows.h> //< GetTickCount()
#include <stdio.h>
#include <stdlib.h>
#define HAVE_SSE2
#define MEXP 19937
#include "SFMT.c"

#define BUF_SZ_BYTES 65536
#define BUF_ARRAY_NUM (BUF_SZ_BYTES/sizeof(uint64_t))
__declspec(align(128)) uint64_t d[BUF_ARRAY_NUM];

int main(int argc, char* argv[])
{
	init_gen_rand(GetTickCount());
	const char *filename = "out.bin";
	if (argc == 2) {
		filename = argv[1];
	}
	FILE *fp = fopen(filename, "ab");
	fseek(fp, 0, SEEK_END);
	__int64 pos = _ftelli64(fp);
	while (true) {
		fill_array64(d, BUF_ARRAY_NUM);

		size_t rv = fwrite((const void *)d, 1, BUF_SZ_BYTES, fp);
		if (rv < BUF_SZ_BYTES) {
			printf("E: rv=%d\n", rv);
			break;
		}
		pos += BUF_SZ_BYTES;
		printf("%lldMB\r", pos>>20);
	}
	fclose(fp);
	return 0;
}

