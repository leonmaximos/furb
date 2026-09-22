import os
import time
import random
import numpy as np

class Ambiente:
    def __init__(self, linhas, colunas):
        self.linhas = linhas
        self.colunas = colunas
        self.matriz = np.ones((linhas, colunas), dtype=int)
        self.matriz[1:linhas-1, 1:colunas-1] = 0
        self.espalharSujeira()

    def espalharSujeira(self):
        for i in range(1, self.linhas - 1):
            for j in range(1, self.colunas - 1):
                if random.random() > 0.5:
                    self.matriz[i, j] = 2

    def obterStatus(self, x, y):
        return self.matriz[x, y]

    def obterSala(self):
        return self.matriz

    def limparSujeira(self, x, y):
        self.matriz[x, y] = 0

    def isParede(self, x, y):
        return self.matriz[x, y] == 1

    def exibir(self, agente_x, agente_y, pontos, limparTerminal=False):
        if limparTerminal:
            os.system('clear' if os.name == 'posix' else 'cls')
        
        for i in range(self.linhas):
            linha_visual = ""
            for j in range(self.colunas):
                if i == agente_x and j == agente_y:
                    linha_visual += "[A]"
                elif self.matriz[i, j] == 1:
                    linha_visual += " 1 "
                elif self.matriz[i, j] == 2:
                    linha_visual += " 2 "
                else:
                    linha_visual += " 0 "
            print(linha_visual)
            
        print(f"Pontos: {pontos}")

def checkObj(sala):
    if np.any(sala == 2):
        return 1
    return 0

def funcaoMapear(x, y):
    if x % 2 != 0:
        if y < 4:
            return 'direita'
        return 'abaixo'
    else:
        if y > 1:
            return 'esquerda'
        return 'abaixo'

def agenteReativoSimples(percepcao):
    x, y, status = percepcao
    if status == 2:
        return 'aspirar'
    return funcaoMapear(x, y)

def agenteObjetivo(percepcao, objObtido):
    if objObtido == 0:
        return 'NoOp'
    return agenteReativoSimples(percepcao)

def executarSimulacao():
    ambiente = Ambiente(6, 6)
    agente_x, agente_y = 1, 1
    pontos = 0

    ambiente.exibir(agente_x, agente_y, pontos, limparTerminal=False)
    
    print("\nEscolha o agente para limpar a sala:")
    print("1. Agente Reativo Simples")
    print("2. Agente Baseado em Objetivos")
    escolha = input("Digite 1 ou 2: ")

    while True:
        ambiente.exibir(agente_x, agente_y, pontos, limparTerminal=True)
        time.sleep(0.3)
        
        statusAtual = ambiente.obterStatus(agente_x, agente_y)
        percepcao = (agente_x, agente_y, statusAtual)
        
        if escolha == '1':
            acao = agenteReativoSimples(percepcao)
            if acao == 'abaixo' and agente_x == 4:
                ambiente.exibir(agente_x, agente_y, pontos, limparTerminal=True)
                break
        else:
            salaAtual = ambiente.obterSala()
            objObtido = checkObj(salaAtual)
            acao = agenteObjetivo(percepcao, objObtido)
            if acao == 'NoOp':
                ambiente.exibir(agente_x, agente_y, pontos, limparTerminal=True)
                break
            
        if acao == 'aspirar':
            ambiente.limparSujeira(agente_x, agente_y)
        elif acao == 'acima' and not ambiente.isParede(agente_x - 1, agente_y):
            agente_x -= 1
        elif acao == 'abaixo' and not ambiente.isParede(agente_x + 1, agente_y):
            agente_x += 1
        elif acao == 'esquerda' and not ambiente.isParede(agente_x, agente_y - 1):
            agente_y -= 1
        elif acao == 'direita' and not ambiente.isParede(agente_x, agente_y + 1):
            agente_y += 1
            
        pontos += 1

    print("\nExecução finalizada com sucesso.")

if __name__ == "__main__":
    executarSimulacao()