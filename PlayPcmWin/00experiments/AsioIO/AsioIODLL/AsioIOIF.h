#ifndef AsioIOIF_H
#define AsioIOIF_H

void AsioDrvInit(void);
void AsioDrvTerm(void);

int AsioDrvGetNumDev(void);
int AsioDrvGetDriverName(int id, char *name_return, unsigned int size);
bool AsioDrvLoadDriver(char *name);
void AsioDrvRemoveCurrentDriver(void);

#endif /* AsioIOIF_H */
