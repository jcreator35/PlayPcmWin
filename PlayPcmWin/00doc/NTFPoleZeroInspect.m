% uses delta sigma toolbox

figure('Name', '5ŽŸNTF‚Ì‹É‚Æ—ë(z•½–Ê)', 'NumberTitle', 'off');
osr=64;
originalSR=44100;
order=5;
opt=1;
H = synthesizeNTF(order, osr, opt);
nH = H.Z{1, 1};
dH = H.P{1, 1};

fprintf('order=%d\n', order);
fprintf(    '    numerator roots=\n');
for i=1:size(nH,1)
    fprintf('        %s,\n', num2str(nH(i),17));
end % i
fprintf(    '    denominator roots=\n');
for i=1:size(dH,1)
    fprintf('        %s,\n', num2str(dH(i),17));
end % i

plotPZ(H);
