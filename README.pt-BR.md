**Português** · [English](README.md)

# UnityBlenderLike

Faz a Scene view da Unity funcionar como o Blender, num pacote só de Editor que você pode manter só para você num projeto compartilhado:

- **G / R / S modais**: mover, girar e escalar seguem o mouse até o clique, e a rotação segue o cursor em volta do pivô, voltas completas incluídas, no lugar do gizmo da Unity, que para de seguir quando você contorna o pivô
- **Travas de eixo e valores digitados**: X / Y / Z travam um eixo no espaço do botão Global / Local da Scene view (de novo para o outro), Shift + X / Y / Z travam tudo menos aquele eixo, digite `90` para um valor exato, Shift dá precisão, Ctrl encaixa, G / R / S trocam de modo sem confirmar, clique confirma, botão direito cancela
- **Teclas seguem o mouse**: com o mouse sobre a Scene view, toda tecla vai para ela, mesmo logo depois de clicar na Hierarchy, como as áreas do Blender
- **Alt + arrastar duplica**: Alt + arrastar uma seta do gizmo de mover da Unity copia a seleção e move a cópia ao longo dela, como no Unreal
- **Teclas de objeto do Blender**: H / Shift+H / Alt+H escondem e mostram, Alt+G / Alt+R / Alt+S zeram transformações, Shift+D duplica e move, Ctrl+P / Alt+P fazem e desfazem parentesco, A / Alt+A selecionam tudo e nada
- **Botão do meio do Blender**: arrastar com o botão do meio orbita em volta do pivô, Shift + botão do meio desloca
- **Vistas do teclado numérico**: 1 / 3 / 7 para frente, direita e topo, Ctrl para o lado oposto, 5 alterna ortográfica, `.` enquadra a seleção, / para a vista local, e Auto Perspective ao orbitar
- **Arrastar sobre as setas da Hierarchy**: aperte uma seta de expandir e arraste para cima ou para baixo, e todo item por onde passar abre (ou fecha) também, como no Outliner do Blender
- **Shift para precisão no Inspector**: segure Shift enquanto arrasta um slider, como a propriedade Range de um material, e ele anda a um décimo da velocidade, como no Blender
- **Inspector de Transform com Z para cima**: Location, Rotation e Scale mostram e editam os valores do painel N do Blender, com Z para cima e o Euler XYZ do Blender, enquanto o objeto guarda os valores da própria Unity por baixo
- **Eixos do Blender**: vistas e travas de eixo batem com modelos exportados do Blender com as configurações padrão de FBX, então o Numpad 7 mostra o que a vista de topo do Blender mostra e R Z gira em volta da vertical
- **Blender Look**: um clique ilumina uma cena URP como o EEVEE do Blender a partir do mesmo HDRI, e os modelos ficam iguais nos dois
- **Nada no projeto**: nenhuma cena, asset ou configuração muda, e ele pode ficar fora do controle de versão, então seus colegas continuam com a Unity padrão. O Blender Look é a única exceção, e só quando você roda: o que ele configura fica no projeto, e os colegas recebem sem ter o pacote

## Sumário

- [Instalação](#instalação)
- [Desinstalação](#desinstalação)
- [Atalhos](#atalhos)
- [Transformações modais](#transformações-modais)
- [Atalhos de objeto](#atalhos-de-objeto)
- [Navegação com o botão do meio](#navegação-com-o-botão-do-meio)
- [Vistas do teclado numérico](#vistas-do-teclado-numérico)
- [Inspector de Transform com Z para cima](#inspector-de-transform-com-z-para-cima)
- [Hierarchy](#hierarchy)
- [Arrasto de precisão](#arrasto-de-precisão)
- [Blender Look](#blender-look)
- [Configurações](#configurações)
- [Conflitos com atalhos existentes](#conflitos-com-atalhos-existentes)
- [Como funciona](#como-funciona)
- [Compatibilidade](#compatibilidade)
- [Por quê](#por-quê)

## Instalação

### Só para você, num projeto compartilhado

Clone o repositório na pasta `Assets` do projeto e deixe-o fora do repositório do projeto:

```
git clone https://github.com/uayten/UnityBlenderLike.git Assets/UnityBlenderLike
```

Depois acrescente estas duas linhas ao `.git/info/exclude` do projeto. Ele funciona como o `.gitignore`, mas fica só na sua máquina, então os arquivos compartilhados do projeto não mudam em nada:

```
/Assets/UnityBlenderLike/
/Assets/UnityBlenderLike.meta
```

Uma linha no `.gitignore` do projeto também serve, se a equipe não se importar de vê-la.

Nada mais no projeto referencia o pacote, então os colegas que não o têm ficam com a Unity padrão, sem scripts faltando nem erros.

### Nos seus próprios projetos

**Window → Package Manager → + → Install package from git URL**:

```
https://github.com/uayten/UnityBlenderLike.git
```

Isso acrescenta uma linha ao `Packages/manifest.json`, então todo mundo que abre o projeto recebe o pacote. Use o primeiro método em projetos compartilhados.

## Desinstalação

Apague a pasta `Assets/UnityBlenderLike` ou remova o pacote no Package Manager. Os atalhos vão junto. As teclas que você mudou em **Edit → Shortcuts** ficam no seu perfil de atalhos.

## Atalhos

Todos os atalhos estão em **Blender Like**, em **Edit → Shortcuts**, onde podem ser trocados. Eles agem na Scene view, e nas outras janelas as mesmas teclas continuam fazendo o que a Unity faz ali.

Como no Blender, as teclas vão para a vista sob o mouse: selecione algo na Hierarchy, passe o mouse sobre a Scene view e aperte G, e o objeto se move, sem clicar na Scene view antes. Isso vale para toda tecla, não só as do pacote: a Scene view recebe o foco do teclado e a tecla, então os atalhos de Scene view da Unity e do seu projeto também respondem, e a Hierarchy não age sobre ela (o End não pula para o último item, por exemplo). Enquanto você digita num campo de texto (renomeando, buscando), as teclas ficam lá, e modificadores sozinhos (Shift, Ctrl, Alt) nunca movem o foco.

| Tecla | Ação | Padrão da Unity que ela substitui na Scene view |
|---|---|---|
| G / R / S | [Mover / girar / escalar modal](#transformações-modais) | R: ferramenta Scale |
| H / Shift+H / Alt+H | [Esconder a seleção / esconder o resto / mostrar tudo](#atalhos-de-objeto) | H: alternar visibilidade, Shift+H: alternar isolamento |
| Alt+G / Alt+R / Alt+S | Zerar posição / rotação / escala | |
| Shift+D | Duplicar e mover | |
| Ctrl+P / Alt+P | Tornar filho do ativo / tirar do pai | Ctrl+P: Play |
| A / Alt+A | Selecionar tudo / nada | |
| Numpad 1 / Ctrl + Numpad 1 | Vista de frente / de trás | |
| Numpad 3 / Ctrl + Numpad 3 | Vista da direita / da esquerda | |
| Numpad 7 / Ctrl + Numpad 7 | Vista de topo / de baixo | |
| Numpad 5 | Alternar ortográfica / perspectiva | |
| Numpad . | Enquadrar a seleção | |
| Numpad / | Vista local | |

W e E continuam com os gizmos de mover e girar da Unity. No macOS, Ctrl é Cmd (o modificador Action da Unity).

## Transformações modais

Selecione algo, aponte para a Scene view e aperte G, R ou S:

| Entrada | Efeito |
|---|---|
| Mover o mouse | Move no plano da vista, gira em volta do eixo da vista seguindo o ângulo do cursor em volta do pivô (voltas completas incluídas) ou escala pela distância ao pivô |
| X / Y / Z | Trava no eixo do espaço escolhido no botão Global / Local da Scene view; aperte de novo para o outro espaço (o local é o do objeto ativo) e mais uma vez para destravar. Como no Blender, onde o primeiro toque segue a orientação de transformação. Uma linha pelo pivô, na cor da Unity para o eixo em que ela corre, mostra a trava |
| Dígitos, `.`, `-`, Backspace | Digita um valor exato: metros para mover (ao longo de X quando nenhum eixo está travado), graus para girar, fator para escalar |
| G / R / S | Troca para outra transformação, mantendo o que a anterior fez |
| Shift + X / Y / Z | Trava em tudo menos aquele eixo, como no Blender: G move no plano dos outros dois (Shift+Z move sobre o chão com os eixos do Blender), S escala os outros dois, R gira em volta daquele eixo. De novo para o plano local, mais uma vez para destravar |
| Shift | Precisão: enquanto segurado, mover e escalar seguem um décimo do movimento do mouse, e girar gira um décimo do ângulo que o cursor percorre em volta do pivô |
| Ctrl | Encaixe: o passo do encaixe de grade da Unity para mover, 5° para girar, 0,1 para escalar |
| Clique esquerdo / Enter | Confirma, como um passo de desfazer |
| Clique direito / Esc | Cancela e volta para onde começou (o Esc age ao ser solto: a Unity 6.6 segura o aperto longe da Scene view) |

Como o grab contínuo do Blender, o cursor nunca fica sem espaço: quando chega a uma borda da Scene view, pula para a borda oposta e a transformação continua sem salto. A linha pontilhada até o pivô segue onde o mouse estaria, além da borda. Durante a transformação, o cursor mostra uma seta de mover, girar ou escalar. O pulo usa a chamada de cursor do sistema operacional (Windows e macOS); no Linux o cursor para na borda como na Unity padrão.

O pivô segue o botão **Pivot / Center** da barra de ferramentas. Com Center, vários objetos giram e escalam em volta do centro comum; com Pivot, cada um gira e cresce em volta da própria origem. Um filho cujo pai também está selecionado se move uma vez, com o pai.

A Unity não consegue escalar um objeto ao longo de um eixo qualquer do mundo sem distorcê-lo, então uma escala travada num eixo age no eixo do próprio objeto que corre mais perto dele.

Durante a transformação, o gizmo de mover / girar / escalar da Unity some, e todas as outras teclas ficam retidas, então X não apaga e os dígitos não trocam de vista. O valor e o eixo atuais aparecem embaixo da Scene view.

Os eixos globais seguem a [configuração de eixos](#configurações). Os eixos locais são os do próprio objeto, com os mesmos nomes: com os eixos do Blender, o Z local é o "para cima" do objeto, como o [Inspector de Transform com Z para cima](#inspector-de-transform-com-z-para-cima) mostra.

## Atalhos de objeto

| Tecla | Efeito |
|---|---|
| H | Esconde a seleção e tudo abaixo dela |
| Shift+H | Esconde tudo menos a seleção |
| Alt+H | Mostra tudo |
| Alt+G / Alt+R / Alt+S | Volta a posição local a 0, a rotação a 0, a escala a 1, como um passo de desfazer |
| Shift+D | Duplica e começa a mover a cópia. O clique direito deixa a cópia no lugar; um desfazer remove a cópia |
| Ctrl+P | Torna os objetos selecionados filhos do ativo, mantendo onde estão. Selecione os filhos primeiro e o pai por último |
| Alt+P | Tira os objetos selecionados dos pais, mantendo onde estão |
| A | Seleciona tudo |
| Alt+A | Seleciona nada |

### Alt + arrastar duplica

Com a ferramenta de mover (W) ou de transformar (Y) da Unity, segure Alt e arraste uma parte do gizmo: a seleção é duplicada e a cópia segue o arrasto, travada como a parte que você pegou (uma seta move ao longo do eixo dela, um quadrado de plano no plano dele, o centro no plano da vista). O original fica onde estava. Solte para confirmar; a duplicação e o movimento se desfazem juntos. Alt + clique sem arrastar desfaz a cópia. Longe do gizmo, Alt + arrastar continua orbitando.

Durante o arrasto, as teclas das [transformações modais](#transformações-modais) também funcionam: X / Y / Z para mudar a trava, dígitos para uma distância exata.

Esconder usa a visibilidade de Scene view da Unity: só afeta o que você vê no Editor, não é salvo na cena e não muda o jogo.

A Unity mantém fixa a hierarquia dentro de uma instância de prefab, então Ctrl+P / Alt+P pulam objetos dentro de uma, com um aviso no Console. Abra o prefab para mudar a hierarquia dele.

## Navegação com o botão do meio

| Gesto | Blender Like | Padrão da Unity |
|---|---|---|
| Arrastar com o botão do meio | Orbita em volta do pivô | Desloca |
| Shift + arrastar com o botão do meio | Desloca: a cena segue o mouse na profundidade do pivô | Desloca |

A órbita gira na mesma velocidade e nas mesmas direções que o Alt + arrastar com o botão esquerdo da Unity. Numa Scene view 2D ou com a rotação travada, arrastar com o botão do meio continua deslocando.

Todo o resto continua da Unity: órbita com Alt + botão esquerdo, botão direito para olhar e voar com WASD, a roda para zoom, e arrastos com o botão do meio junto com Alt ou Ctrl. Durante um G / R / S, o botão do meio cancela, como o direito.

Desligue em **Middle Mouse Navigation**, nas [Configurações](#configurações).

## Vistas do teclado numérico

| Tecla | Vista |
|---|---|
| Numpad 1 | Frente (ortográfica) |
| Ctrl + Numpad 1 | Trás |
| Numpad 3 | Direita |
| Ctrl + Numpad 3 | Esquerda |
| Numpad 7 | Topo |
| Ctrl + Numpad 7 | Baixo |
| Numpad 5 | Alterna ortográfica / perspectiva |
| Numpad . | Enquadra a seleção, como o F da Unity |
| Numpad / | Vista local: só a seleção aparece, enquadrada. De novo para mostrar tudo e voltar à vista de antes |

As vistas mantêm o pivô e o zoom atuais, e seguem a [configuração de eixos](#configurações).

Como o Auto Perspective do Blender, uma vista que ficou ortográfica pelo 1, 3 ou 7 volta à perspectiva quando você orbita. Uma vista que ficou ortográfica pelo 5 continua ortográfica ao orbitar.

A vista local usa o isolamento da Unity, o mesmo do atalho de isolamento dela: não é salva na cena.

## Inspector de Transform com Z para cima

Com os eixos do Blender, o componente Transform no Inspector mostra os valores do Blender:

| Linha | Mostra |
|---|---|
| **Location** | Posição com Z para cima e Y de frente para trás: X do Blender = -X da Unity, Y do Blender = -Z da Unity, Z do Blender = Y da Unity |
| **Rotation** | Os ângulos Euler XYZ do Blender, em graus, girando para o mesmo lado que no Blender |
| **Scale** | Escala com Y e Z trocados |

Os rótulos dizem Location / Rotation / Scale, como no painel N do Blender, para ficar claro quais valores você está vendo. A edição funciona como sempre: digitar, arrastar os rótulos X / Y / Z, expressões, vários objetos de uma vez (um valor diferente entre eles aparece como `—`), overrides de prefab em negrito com reverter pelo botão direito, e Undo.

Só a exibição muda. O objeto guarda os valores da Unity, então cenas, prefabs, animação e código ficam intactos, e um colega sem o pacote vê os valores da Unity para o mesmo objeto. Ao falar de posições com ele, lembre que o seu Z é o Y dele.

Para ver os valores da Unity, clique com o botão direito no cabeçalho do Transform → **Blender Like: Z-Up Values**, ou desligue nas [Configurações](#configurações). Os gizmos da Scene view continuam os da Unity: a seta para cima continua sendo a verde.

Os valores batem com o painel N do Blender para objetos cujo FBX não traz rotação de eixo própria. Quando a raiz de um modelo entra com rotação X -90 (importação de FBX sem **Bake Axis Conversion**), o Inspector mostra esse -90, porque ele está mesmo lá na Unity.

O RectTransform (UI) mantém o Inspector da Unity.

## Hierarchy

Aperte a seta de expandir de um item na Hierarchy e arraste para cima ou para baixo: todo item por onde o mouse passa fica no mesmo estado do primeiro, então um único traço abre (ou fecha) uma coluna inteira de setas, como no Outliner do Blender. Itens sem filhos são pulados.

Abrir um item empurra as linhas de baixo, então um arrasto para baixo esbarra nos filhos que acabou de revelar, como no Blender. Para abrir uma lista de irmãos de uma vez, comece pelo de baixo e arraste para cima.

Alt+clique mantém o comportamento da Unity (expandir ou recolher tudo abaixo do item).

Isso funciona com a janela Hierarchy da Unity 6 (a feita com UI Toolkit). O pacote encontra as peças internas dessa janela pelo nome, então em versões da Unity com a Hierarchy antiga nada muda ali.

## Arrasto de precisão

Segure Shift enquanto arrasta um slider no Inspector, como a propriedade Range de um material, e o valor muda a um décimo da velocidade do mouse, como o Shift faz nos sliders do Blender. Dá para apertar ou soltar o Shift no meio do arrasto: o valor fica onde está e continua na nova velocidade, então a alça do slider só volta a ficar sob o cursor quando você solta.

Arrastos que já tratam o Shift do próprio jeito, como arrastar o rótulo de um campo numérico (a Unity acelera ali), não são afetados.

## Blender Look

**Window → Blender Like → Blender Look** configura a cena ativa para que um modelo iluminado por um HDRI fique igual na Unity e no EEVEE do Blender, Material Preview incluído, com a view transform Standard. Escolha o mesmo `.exr` ou `.hdr` do mundo do Blender, importado como Cubemap, e aperte **Apply to Active Scene**. A janela abre preenchida com o skybox da própria cena, então rodar de novo atualiza esse skybox no lugar.

O que ela muda, tudo no projeto, onde os colegas recebem sem ter o pacote:

| Parte | Por quê |
|---|---|
| Uma cópia do HDRI cortada no threshold do sol (`<nome>_eevee.exr`, ao lado do original), num material Skybox/Cubemap com Tint 0,5 e Rotation 90 | O EEVEE transforma todo pixel mais claro que World → Sun → Threshold numa luz de sol e ilumina com o resto. O Skybox/Cubemap dobra o Tint, então 0,5 mantém o brilho do próprio HDRI, e o Rotation 90 alinha com o mundo do Blender para exportações FBX padrão |
| Uma luz direcional com a energia que o corte tirou, vinda da direção de onde ela veio, sem sombra como no padrão do EEVEE | É o sol do EEVEE. A luz direcional da cena é reaproveitada, se houver |
| `AmbientProbeL1.cs`, um script acrescentado em `Assets/BlenderLook/` se o projeto ainda não tiver um | O EEVEE ilumina superfícies difusas com esféricos harmônicos L1; a luz ambiente da Unity usa L2, que escurece tudo o que está voltado para baixo. O script descarta a parte L2 sempre que a Unity carrega a luz ambiente, no Editor e nos builds |
| Tonemapping None e Vignette 0 no Volume Profile do asset da URP | A view transform Standard do Blender. Volumes da cena que definem outro tonemapping são listados depois de aplicar |
| O SSAO do renderer com raio pequeno | O EEVEE só escurece vãos pequenos. Um raio perto do tamanho do modelo escurece um lado inteiro; cerca de um oitavo da altura dos modelos funciona |
| Generate Lighting | A luz ambiente e os reflexos só se atualizam quando a iluminação é gerada |

Funciona com qualquer HDRI: o corte, a energia e a direção do sol são calculados a partir do próprio arquivo. A Exposure começa em 0,95, valor que bateu com o EEVEE em renders lado a lado do mesmo modelo com três HDRIs do Blender (forest, sunset e studio). Strength é a força do Background do mundo no Blender. O Material Preview do Blender gira o HDRI junto com a vista, a menos que **World Space Lighting** esteja ligado no menu de shading do viewport, então ligue antes de comparar ângulos.

Precisa de URP 17 (Unity 6) ou mais nova; em outros projetos a janela não aparece.

## Configurações

**Edit → Preferences → Blender Like**:

| Configuração | Efeito |
|---|---|
| **Axes: Blender** (padrão) | As vistas batem com o Blender para modelos exportados com as configurações padrão de FBX: o Numpad 7 mostra o modelo como a vista de topo do Blender. X / Y / Z usam os nomes do Blender: Z para cima, Y de trás para frente, e G Z 1 ou R Z 90 vão para o mesmo lado que no Blender |
| **Axes: Unity** | As vistas usam a orientação da própria Unity, a mesma do gizmo da Scene view. X / Y / Z usam os nomes da Unity: Y para cima, Z para frente |
| **Z-Up Transform Inspector** (ligado por padrão) | Com os eixos do Blender, o [Inspector de Transform](#inspector-de-transform-com-z-para-cima) mostra os valores do Blender |
| **Auto Perspective** (ligado por padrão) | Vistas que ficaram ortográficas pelo 1, 3 ou 7 voltam à perspectiva ao orbitar |
| **Middle Mouse Navigation** (ligado por padrão) | [Botão do meio orbita, Shift + botão do meio desloca](#navegação-com-o-botão-do-meio) |
| **Alt + Drag Duplicates** (ligado por padrão) | [Alt + arrastar no gizmo de mover duplica](#alt--arrastar-duplica) |
| **Shift Precision Drag** (ligado por padrão) | [Shift deixa os sliders do Inspector mais lentos](#arrasto-de-precisão) |

Por que o Blender precisa de uma configuração de eixos própria: uma exportação FBX padrão do Blender leva o +Y do Blender para o -Z da Unity. A vista de topo da Unity põe o +Z no alto da tela, então uma planta desenhada no Blender aparece de cabeça para baixo nela.

As configurações são salvas por projeto, só na sua máquina (em `UserSettings/`, que os projetos Unity deixam fora do controle de versão).

## Conflitos com atalhos existentes

Quando um atalho de Scene view e um global usam a mesma tecla, a Unity roda o da Scene view enquanto ela está com o foco. Então G, R, S, Ctrl+P e os outros substituem as teclas globais da Unity só ali; as teclas do modo de voo (botão direito + WASD) continuam funcionando.

Dois padrões da Unity ficam num contexto próprio, ativo também na Scene view, então eles conflitam com o pacote:

- **Scene Visibility → Toggle Selection And Descendants Visibility** (H)
- **Scene Visibility → Toggle Isolation On Selection And Descendants** (Shift+H)

Limpe as teclas deles em **Edit → Shortcuts** (o pacote cobre os dois com H e Numpad /). O mesmo vale para qualquer **Scene View → Set Orthographic ... View** ou **Toggle Orthographic Projection** da Unity que você tenha posto no teclado numérico.

## Como funciona

- O pacote é um assembly de Editor (`UnityBlenderLike.Editor`), mais o `UnityBlenderLike.Editor.BlenderLook`, que só compila com URP 17 ou mais nova. Nada roda nos builds nem em tempo de execução, e nenhuma cena ou asset se refere a ele. O Blender Look grava o script de tempo de execução dele no projeto como código-fonte comum, então os builds não precisam do pacote.
- As teclas são atalhos comuns do Shortcut Manager no contexto da Scene view, então aparecem e podem ser trocadas em **Edit → Shortcuts**.
- Enquanto uma transformação modal roda, ela lê as teclas antes de qualquer atalho: do painel UI Toolkit da Scene view, que é onde a Unity 6.6 as entrega, e de um handler à frente do Shortcut Manager, para versões que as entregam do jeito IMGUI. Os dois são removidos quando a transformação termina.
- O Alt + arrastar duplica descobre qual parte do gizmo de mover está sob o mouse a cada evento da Scene view, e pega o Alt + aperto do painel da Scene view antes que o atalho de órbita Alt + arrastar da Unity o veja. O arrasto é o próprio mover do pacote, travado como a parte do gizmo.
- A navegação com o botão do meio lê o mouse no `beforeSceneGui` da Scene view, antes dos controles de câmera dela, e move o pivô e a rotação da vista.
- As teclas seguem o mouse por dois ouvintes, porque as teclas chegam às janelas de dois jeitos: janelas IMGUI as recebem pelo handler do Shortcut Manager (um segundo handler vai à frente dele), e janelas UI Toolkit, como a Hierarchy da Unity 6, as recebem direto do painel (um ouvinte na raiz de cada painel as pega primeiro). Qualquer um dos dois dá o foco à Scene view e manda a tecla para ela. A Game view mantém as teclas, então jogar não é interrompido quando o mouse passa sobre a Scene view.
- Confirmar põe todo objeto de volta onde começou, registra para o Undo e então aplica a transformação final, então o arrasto inteiro se desfaz num passo. O Shift+D junta a duplicação nesse mesmo passo.
- O Auto Perspective lê a animação de vista da Scene view (interna) para distingui-la de uma órbita.
- O arrasto na Hierarchy escuta os eventos de ponteiro da janela antes das linhas, e abre ou fecha itens pelos métodos de expandir e recolher da vista da Hierarchy.
- O arrasto de precisão envolve o GUI de cada contêiner IMGUI do Inspector, então vê um arrasto antes dos controles. Sliders IMGUI tiram o valor da distância que o mouse andou desde o aperto, então, enquanto o Shift está segurado, a posição do mouse que eles recebem é virtual e avança um décimo do movimento real.
- O Inspector de Transform com Z para cima é um Inspector personalizado para o Transform. Ele converte os valores na entrada e na saída, e desenha o Inspector de Transform da própria Unity quando está desligado.

## Compatibilidade

Escrito e testado na Unity 6 (6000.6) no Windows. Só usa APIs de Editor que existem desde a Unity 2021.3, o mínimo declarado no `package.json`, mas versões mais antigas não foram testadas.

Algumas funções leem campos internos da Unity. Se uma versão futura os remover:

- `EditorApplication.globalEventHandler`: mover o mouse e confirmar ou cancelar com um clique continuam funcionando, mas as teclas durante uma transformação modal (travas de eixo, valores digitados, Enter, Esc) vão para os atalhos da própria Unity, e as teclas param de seguir o mouse (clique na Scene view antes).
- A animação de vista da Scene view: o Auto Perspective espera um segundo fixo depois de uma tecla de vista antes de começar a observar órbitas.
- Os itens de linha da janela Hierarchy (`Unity.Hierarchy.HierarchyViewItem`): se mudarem, arrastar sobre as setas para de fazer qualquer coisa e um clique comum funciona como na Unity padrão.
- `UnityEditor.TransformInspector`, o Inspector de Transform da própria Unity: com o Inspector de Z para cima desligado, o Transform mostra os campos crus (rotação como quaternion).

## Por quê

Vindo do Blender, a Scene view da Unity vivia atrapalhando. O gizmo de girar da Unity para de seguir o mouse quando você contorna o pivô, o que deixa uma simples meia-volta desajeitada. Plantas modeladas no Blender apareciam de cabeça para baixo na vista de topo da Unity. E mãos treinadas em G, R, S e H viviam buscando teclas que fazem outra coisa. Este pacote traz esses hábitos, e os mantém fora do projeto compartilhado, do mesmo jeito que o [MayaBlenderLike](https://github.com/uayten/MayaBlenderLike) faz para o Maya.
