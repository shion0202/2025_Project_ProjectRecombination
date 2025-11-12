#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

float Rec(float num) {
    return 1 / num;
}

void EdgeMask_float(float taper, float x, float lineDistance, out float edgeMask) {
    if (lineDistance < 0.001) {
        lineDistance = 1;
    }

    float edgeTaperNormalized = lineDistance * taper;
    float invEdgeTaperNormalized = Rec(edgeTaperNormalized);
    edgeMask = 1;

    if (x >= 0 && x <= invEdgeTaperNormalized) {
        edgeMask = edgeTaperNormalized * x;
    } else if (x >= 1 - invEdgeTaperNormalized && x <= 1) {
        edgeMask = -edgeTaperNormalized * x + edgeTaperNormalized;
    }
}

#endif