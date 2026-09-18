import numpy as np

def exibir(matriz):
    for i in range(matriz.shape[0]):
        linha_visual = ""
        for j in range(matriz.shape[1]):
            if matriz[i, j] == 1:
                linha_visual += " 1 "
            else:
                linha_visual += " 0 "
        print(linha_visual)

ambiente = np.ones((6, 6))
ambiente[1:5, 1:5] = 0

exibir(ambiente)