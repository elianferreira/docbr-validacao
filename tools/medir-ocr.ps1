<#
.SYNOPSIS
  Mede a taxa de acerto do OCR do DocBr.Web sobre uma CNH-amostra submetida a
  degradações típicas de foto de celular.

.DESCRIPTION
  Gera uma amostra sintética com layout, fundo guilhochado e marca d'água de teste,
  aplica uma bateria de degradações (compressão, redução de resolução, rotação,
  desfoque, ruído, contraste baixo, iluminação irregular) e envia cada variante para
  a API, conferindo se o número verdadeiro foi encontrado e em que posição ficou.

  O objetivo é responder "o Tesseract serve para foto de CNH real?" sem precisar de
  um documento real — nenhuma imagem produzida aqui corresponde a uma pessoa.

.PARAMETER Url
  Endereço base da API já em execução.

.PARAMETER Saida
  Pasta onde as variantes são gravadas, para inspeção visual.

.EXAMPLE
  .\tools\medir-ocr.ps1
#>
[CmdletBinding()]
param(
    [string] $Url = 'http://localhost:5199',
    [string] $Saida = (Join-Path $env:TEMP 'docbr-ocr-medicao')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Número de registro da amostra. Válido no módulo 11, mas sintético.
$NumeroVerdadeiro = '35440631331'

New-Item -ItemType Directory -Force -Path $Saida | Out-Null

function New-AmostraCnh {
    <#
      Desenha uma CNH-amostra em alta resolução: fundo guilhochado (as ondas que
      atrapalham o OCR em documento real), campos na posição do documento oficial,
      retângulo no lugar da foto e marca d'água deixando claro que é amostra.
    #>
    param([int] $Largura = 1600, [int] $Altura = 1000)

    $bitmap = New-Object System.Drawing.Bitmap $Largura, $Altura
    $g = [System.Drawing.Graphics]::FromImage($bitmap)
    $g.SmoothingMode = 'AntiAlias'
    $g.TextRenderingHint = 'ClearTypeGridFit'
    $g.Clear([System.Drawing.Color]::FromArgb(246, 244, 236))

    # Guilhoché: senóides sobrepostas, o mesmo tipo de fundo de segurança que reduz
    # o contraste efetivo do texto numa foto.
    $canetaFundo = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(38, 120, 150, 110)), 1
    for ($deslocamento = -200; $deslocamento -lt $Altura + 200; $deslocamento += 14) {
        $pontos = New-Object 'System.Collections.Generic.List[System.Drawing.PointF]'
        for ($x = 0; $x -le $Largura; $x += 8) {
            $y = $deslocamento + 26 * [Math]::Sin($x / 70.0) + 14 * [Math]::Sin($x / 23.0)
            $pontos.Add((New-Object System.Drawing.PointF $x, $y))
        }
        $g.DrawCurve($canetaFundo, $pontos.ToArray())
    }

    $tinta = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(22, 28, 36))
    $tintaRotulo = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(92, 102, 112))

    $fonteTitulo = New-Object System.Drawing.Font 'Arial', 21, ([System.Drawing.FontStyle]::Bold)
    $fonteRotulo = New-Object System.Drawing.Font 'Arial', 11
    $fonteValor = New-Object System.Drawing.Font 'Arial', 17
    $fonteRegistro = New-Object System.Drawing.Font 'Arial', 22, ([System.Drawing.FontStyle]::Bold)

    $g.DrawString('REPÚBLICA FEDERATIVA DO BRASIL', $fonteRotulo, $tintaRotulo, 60, 40)
    $g.DrawString('CARTEIRA NACIONAL DE HABILITAÇÃO', $fonteTitulo, $tinta, 60, 62)

    # Lugar da foto: na CNH real ele rouba contraste da metade esquerda.
    $g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(205, 208, 212))), 60, 140, 260, 330)
    $g.DrawString('FOTO', $fonteRotulo, $tintaRotulo, 160, 295)

    $campos = @(
        @{ Rotulo = 'NOME';                              Valor = 'MARIA APARECIDA DE SOUZA'; X = 360; Y = 150 },
        @{ Rotulo = 'DOC. IDENTIDADE / ORG. EMISSOR / UF'; Valor = '12.345.678-9 SSP SC';     X = 360; Y = 240 },
        @{ Rotulo = 'CPF';                               Valor = '123.456.789-09';           X = 360; Y = 330 },
        @{ Rotulo = 'DATA NASCIMENTO';                   Valor = '07/11/1984';               X = 360; Y = 420 },
        @{ Rotulo = 'VALIDADE';                          Valor = '14/03/2029';               X = 980; Y = 240 },
        @{ Rotulo = '1ª HABILITAÇÃO';                    Valor = '02/08/2011';               X = 980; Y = 330 },
        @{ Rotulo = 'CAT. HAB.';                         Valor = 'AB';                       X = 980; Y = 420 }
    )

    foreach ($campo in $campos) {
        $g.DrawString($campo.Rotulo, $fonteRotulo, $tintaRotulo, $campo.X, $campo.Y)
        $g.DrawString($campo.Valor, $fonteValor, $tinta, $campo.X, ($campo.Y + 22))
    }

    $g.DrawString('Nº REGISTRO', $fonteRotulo, $tintaRotulo, 980, 150)
    $g.DrawString($NumeroVerdadeiro, $fonteRegistro, $tinta, 980, 172)

    # Marca d'água: esta imagem não pode ser confundida com documento.
    $fonteMarca = New-Object System.Drawing.Font 'Arial', 62, ([System.Drawing.FontStyle]::Bold)
    $tintaMarca = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(48, 190, 60, 60))
    $estado = $g.Save()
    $g.TranslateTransform(($Largura / 2), ($Altura / 2))
    $g.RotateTransform(-22)
    $formato = New-Object System.Drawing.StringFormat
    $formato.Alignment = 'Center'
    $g.DrawString('AMOSTRA PARA TESTE DE OCR', $fonteMarca, $tintaMarca, 0, -40, $formato)
    $g.Restore($estado)

    $g.Dispose()
    return $bitmap
}

function Save-Jpeg {
    param([System.Drawing.Bitmap] $Bitmap, [string] $Caminho, [int] $Qualidade)

    $codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
        Where-Object { $_.MimeType -eq 'image/jpeg' }
    $parametros = New-Object System.Drawing.Imaging.EncoderParameters 1
    $parametros.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter (
        [System.Drawing.Imaging.Encoder]::Quality, [int64] $Qualidade)
    $Bitmap.Save($Caminho, $codec, $parametros)
}

function Resize-Bitmap {
    param([System.Drawing.Bitmap] $Bitmap, [int] $LarguraAlvo)

    $escala = $LarguraAlvo / $Bitmap.Width
    $destino = New-Object System.Drawing.Bitmap $LarguraAlvo, ([int]($Bitmap.Height * $escala))
    $g = [System.Drawing.Graphics]::FromImage($destino)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.DrawImage($Bitmap, 0, 0, $destino.Width, $destino.Height)
    $g.Dispose()
    return $destino
}

function Rotate-Bitmap {
    param([System.Drawing.Bitmap] $Bitmap, [single] $Graus)

    $destino = New-Object System.Drawing.Bitmap $Bitmap.Width, $Bitmap.Height
    $g = [System.Drawing.Graphics]::FromImage($destino)
    $g.Clear([System.Drawing.Color]::FromArgb(240, 238, 232))
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.TranslateTransform(($Bitmap.Width / 2), ($Bitmap.Height / 2))
    $g.RotateTransform($Graus)
    $g.TranslateTransform((-$Bitmap.Width / 2), (-$Bitmap.Height / 2))
    $g.DrawImage($Bitmap, 0, 0)
    $g.Dispose()
    return $destino
}

function Add-Ruido {
    <# Ruído por pixel, como o de foto com pouca luz. #>
    param([System.Drawing.Bitmap] $Bitmap, [int] $Intensidade)

    $aleatorio = New-Object System.Random 7
    $destino = New-Object System.Drawing.Bitmap $Bitmap
    for ($y = 0; $y -lt $destino.Height; $y += 1) {
        for ($x = 0; $x -lt $destino.Width; $x += 1) {
            $cor = $destino.GetPixel($x, $y)
            $delta = $aleatorio.Next(-$Intensidade, $Intensidade)
            $destino.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(
                [Math]::Clamp($cor.R + $delta, 0, 255),
                [Math]::Clamp($cor.G + $delta, 0, 255),
                [Math]::Clamp($cor.B + $delta, 0, 255)))
        }
    }
    return $destino
}

function Set-Contraste {
    <# Achata a faixa dinâmica, como foto contra a luz. #>
    param([System.Drawing.Bitmap] $Bitmap, [double] $Fator)

    $destino = New-Object System.Drawing.Bitmap $Bitmap.Width, $Bitmap.Height
    $g = [System.Drawing.Graphics]::FromImage($destino)
    $matriz = New-Object System.Drawing.Imaging.ColorMatrix
    $matriz.Matrix00 = $Fator; $matriz.Matrix11 = $Fator; $matriz.Matrix22 = $Fator
    $deslocamento = (1 - $Fator) / 2
    $matriz.Matrix40 = $deslocamento; $matriz.Matrix41 = $deslocamento; $matriz.Matrix42 = $deslocamento
    $atributos = New-Object System.Drawing.Imaging.ImageAttributes
    $atributos.SetColorMatrix($matriz)
    $g.DrawImage($Bitmap, (New-Object System.Drawing.Rectangle 0, 0, $destino.Width, $destino.Height),
        0, 0, $Bitmap.Width, $Bitmap.Height, [System.Drawing.GraphicsUnit]::Pixel, $atributos)
    $g.Dispose()
    return $destino
}

function Add-Desfoque {
    <# Desfoque barato: reduz e amplia de volta, o efeito de foto fora de foco. #>
    param([System.Drawing.Bitmap] $Bitmap, [double] $Fator)

    $pequeno = Resize-Bitmap -Bitmap $Bitmap -LarguraAlvo ([int]($Bitmap.Width * $Fator))
    $destino = New-Object System.Drawing.Bitmap $Bitmap.Width, $Bitmap.Height
    $g = [System.Drawing.Graphics]::FromImage($destino)
    $g.InterpolationMode = 'HighQualityBicubic'
    $g.DrawImage($pequeno, 0, 0, $destino.Width, $destino.Height)
    $g.Dispose()
    $pequeno.Dispose()
    return $destino
}

function Add-IluminacaoIrregular {
    <# Gradiente escurecendo um canto, como sombra da mão sobre o documento. #>
    param([System.Drawing.Bitmap] $Bitmap)

    $destino = New-Object System.Drawing.Bitmap $Bitmap
    $g = [System.Drawing.Graphics]::FromImage($destino)
    $pincel = New-Object System.Drawing.Drawing2D.LinearGradientBrush (
        (New-Object System.Drawing.Point 0, 0),
        (New-Object System.Drawing.Point $destino.Width, $destino.Height),
        [System.Drawing.Color]::FromArgb(0, 0, 0, 0),
        [System.Drawing.Color]::FromArgb(135, 0, 0, 0))
    $g.FillRectangle($pincel, 0, 0, $destino.Width, $destino.Height)
    $g.Dispose()
    return $destino
}

Write-Host 'Gerando a amostra e as variantes...' -ForegroundColor Cyan
$original = New-AmostraCnh

$variantes = [ordered] @{}

$variantes['01 - original 1600px PNG'] = { param($b) New-Object System.Drawing.Bitmap $b }
$variantes['02 - reduzida 1000px']     = { param($b) Resize-Bitmap -Bitmap $b -LarguraAlvo 1000 }
$variantes['03 - reduzida 700px']      = { param($b) Resize-Bitmap -Bitmap $b -LarguraAlvo 700 }
$variantes['04 - rotacionada 2 graus'] = { param($b) Rotate-Bitmap -Bitmap $b -Graus 2 }
$variantes['05 - rotacionada 7 graus'] = { param($b) Rotate-Bitmap -Bitmap $b -Graus 7 }
$variantes['06 - contraste baixo']     = { param($b) Set-Contraste -Bitmap $b -Fator 0.45 }
$variantes['07 - desfocada']           = { param($b) Add-Desfoque -Bitmap $b -Fator 0.42 }
$variantes['08 - iluminacao irregular']= { param($b) Add-IluminacaoIrregular -Bitmap $b }
$variantes['09 - ruido de sensor']     = { param($b) Add-Ruido -Bitmap (Resize-Bitmap -Bitmap $b -LarguraAlvo 900) -Intensidade 45 }
$variantes['10 - celular realista']    = {
    param($b)
    $passo = Resize-Bitmap -Bitmap $b -LarguraAlvo 1100
    $passo = Rotate-Bitmap -Bitmap $passo -Graus 3
    $passo = Add-Desfoque -Bitmap $passo -Fator 0.55
    return Add-IluminacaoIrregular -Bitmap $passo
}

$arquivos = [ordered] @{}

foreach ($nome in $variantes.Keys) {
    $bitmap = & $variantes[$nome] $original
    $caminho = Join-Path $Saida ("{0}.jpg" -f ($nome -replace '[^a-zA-Z0-9]+', '_'))

    # Toda variante passa por JPEG: foto de celular nunca chega sem compressão.
    $qualidade = if ($nome -like '*celular*' -or $nome -like '*ruido*') { 55 } else { 85 }
    Save-Jpeg -Bitmap $bitmap -Caminho $caminho -Qualidade $qualidade

    $arquivos[$nome] = $caminho
    $bitmap.Dispose()
}

$original.Dispose()

Write-Host "Variantes gravadas em $Saida" -ForegroundColor Cyan
Write-Host ''

$linhas = foreach ($nome in $arquivos.Keys) {
    $caminho = $arquivos[$nome]

    try {
        $resposta = Invoke-RestMethod -Uri "$Url/api/documentos/cnh/arquivo" -Method Post `
            -Form @{ arquivo = Get-Item $caminho } -TimeoutSec 120
    }
    catch {
        [pscustomobject] @{
            Variante = $nome; KB = 0; Achou = 'ERRO'; Posicao = '-'; Primeiro = $_.Exception.Message
        }
        continue
    }

    $todos = @($resposta.candidatos)
    $indice = [array]::FindIndex([object[]] $todos, [Predicate[object]] { $args[0].numero -eq $NumeroVerdadeiro })
    $primeiro = if ($todos.Count) { $todos[0].numero } else { '(nenhum)' }

    [pscustomobject] @{
        Variante = $nome
        KB       = [int]((Get-Item $caminho).Length / 1KB)
        Achou    = if ($indice -ge 0) { 'sim' } else { 'NAO' }
        Posicao  = if ($indice -ge 0) { $indice + 1 } else { '-' }
        Primeiro = $primeiro
    }
}

$linhas | Format-Table -AutoSize

$total = @($linhas).Count
$achou = @($linhas | Where-Object { $_.Achou -eq 'sim' }).Count
$topo = @($linhas | Where-Object { $_.Posicao -eq 1 }).Count

Write-Host ''
Write-Host ("Encontrou o numero verdadeiro : {0}/{1}" -f $achou, $total) -ForegroundColor Yellow
Write-Host ("Ranqueou em primeiro lugar    : {0}/{1}" -f $topo, $total) -ForegroundColor Yellow
