public MyMemcpy64

STACKBYTES    equ 16*2

.code

SaveRegisters MACRO
    sub rsp,STACKBYTES
   .allocstack STACKBYTES
    movdqu [rsp+16*0],xmm6  ; cannot use movdqa because rsp is not 16-byte aligned
   .savexmm128 xmm6, 16*0
    movdqu [rsp+16*1],xmm7
   .savexmm128 xmm7, 16*1
   .endprolog
ENDM

RestoreRegisters MACRO
    movdqu xmm6, [rsp+16*0]
    movdqu xmm7, [rsp+16*1]
    add rsp,STACKBYTES
ENDM

; MyMemcpy64(char *dst, const char *src, int bytes)
; dst   --> rcx
; src   --> rdx
; bytes --> r8d
align 8
MyMemcpy64 proc frame
    SaveRegisters
    mov rax, rcx ; move dst address from rcx to rax (src address is rdx)
    mov ecx, r8d ; move loop parameter(bytes argument) from r8d to rcx
    add rax, rcx ; now rax points end of dst buffer
    add rdx, rcx ; now rdx points end of src buffer
    neg rcx      ; now rdx+rcx points start of src buffer and rax+rcx points start of dst buffer
align 8
LabelBegin:
    movdqa xmm0, [rdx+rcx    ]
    movdqa xmm1, [rdx+rcx+10H]
    movdqa xmm2, [rdx+rcx+20H]
    movdqa xmm3, [rdx+rcx+30H]
    movdqa xmm4, [rdx+rcx+40H]
    movdqa xmm5, [rdx+rcx+50H]
    movdqa xmm6, [rdx+rcx+60H]
    movdqa xmm7, [rdx+rcx+70H]
    movdqa [rax+rcx    ], xmm0
    movdqa [rax+rcx+10H], xmm1
    movdqa [rax+rcx+20H], xmm2
    movdqa [rax+rcx+30H], xmm3
    movdqa [rax+rcx+40H], xmm4
    movdqa [rax+rcx+50H], xmm5
    movdqa [rax+rcx+60H], xmm6
    movdqa [rax+rcx+70H], xmm7
    add rcx, 80H
    jnz LabelBegin
    RestoreRegisters
    ret
align 8
MyMemcpy64 endp
end

