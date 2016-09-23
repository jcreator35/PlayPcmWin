public MyMemcpy64a

STACKBYTES    equ 16*3

.code

SaveRegisters MACRO
    sub rsp,STACKBYTES
   .allocstack STACKBYTES
    movdqu [rsp+16*0],xmm6
   .savexmm128 xmm6, 16*0
    movdqu [rsp+16*1],xmm7
   .savexmm128 xmm7, 16*1
    mov [rsp+16*2],rsi
   .savereg rsi,16*2
    mov [rsp+16*2+8],rdi
   .savereg rdi,16*2+8
   .endprolog
ENDM
 
RestoreRegisters MACRO
    movdqu xmm6, [rsp+16*0]
    movdqu xmm7, [rsp+16*1]
    mov rsi, [rsp+16*2]
    mov rdi, [rsp+16*2+8]
    add rsp,STACKBYTES
ENDM
 
; MyMemcpy64a(char *dst, const char *src, int bytes)
 ; dst   --> rcx
 ; src   --> rdx
 ; bytes --> r8d
 align 8
 MyMemcpy64a proc frame
    SaveRegisters
     mov rsi, rdx ; src pointer
     mov rdi, rcx ; dest pointer
     mov ecx, r8d ; copy bytes (multiply of 128)
     shr ecx, 7   ; divide by 128
 align 8
 LabelBegin:
     prefetchnta 128[rsi]
     prefetchnta 192[rsi]

     movdqa xmm0, 0[rsi]
     movdqa xmm1, 16[rsi]
     movdqa xmm2, 32[rsi]
     movdqa xmm3, 48[rsi]
     movdqa xmm4, 64[rsi]
     movdqa xmm5, 80[rsi]
     movdqa xmm6, 96[rsi]
     movdqa xmm7, 112[rsi]

     movdqa 0[rdi],   xmm0
     movdqa 16[rdi],  xmm1
     movdqa 32[rdi],  xmm2
     movdqa 48[rdi],  xmm3
     movdqa 64[rdi],  xmm4
     movdqa 80[rdi],  xmm5
     movdqa 96[rdi],  xmm6
     movdqa 112[rdi], xmm7

     add rsi, 128
     add rdi, 128
     dec ecx
     jnz LabelBegin
     RestoreRegisters
     ret
 align 8
 MyMemcpy64a endp
 end