"""Gera vetores de teste executando a implementação de referência (validate-docbr)."""
import json
import random
import sys

sys.path.insert(0, r"C:\Users\7321856\AppData\Local\Temp\docbr-ref")

from validate_docbr import CPF, CNPJ, CNH, CNS, PIS, RENAVAM, TituloEleitoral, Certidao

random.seed(20260915)

TIPOS = {
    "cpf": CPF(),
    "cnpj": CNPJ(),
    "cnh": CNH(),
    "cns": CNS(),
    "pis": PIS(),
    "renavam": RENAVAM(),
    "titulo-eleitor": TituloEleitoral(),
    "certidao": Certidao(),
}

saida = {}

for slug, validador in TIPOS.items():
    validos = []
    while len(validos) < 400:
        doc = validador.generate()
        if validador.validate(doc):
            validos.append(doc)

    # Amostra estável para virar InlineData no xUnit.
    saida[slug] = {
        "validos": validos,
        "amostra": validos[:8],
    }

# CNPJ alfanumérico é um caso à parte da geração padrão.
cnpj = CNPJ()
alfa = []
while len(alfa) < 200:
    doc = cnpj.generate(digits_only=False)
    if cnpj.validate(doc):
        alfa.append(doc)
saida["cnpj-alfanumerico"] = {"validos": alfa, "amostra": alfa[:8]}

print(json.dumps(saida, ensure_ascii=False))
