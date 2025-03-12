using SimpleC.Types;
using SimpleC.Types.AstNodes;
using SkiaSharp;

public class AstFlowchartGenerator
{
    private SKCanvas canvas;
    private float nodeWidth = 150;
    private float nodeHeight = 80;
    // Aumentar el espaciado vertical y horizontal
    private float verticalSpacing = 100;
    private float horizontalSpacing = 200;
    private SKFont textFont;
    private SKPaint textPaint;
    private SKFont subtextFont;
    private SKPaint subtextPaint;
    private float minX = float.MaxValue;
    private float minY = float.MaxValue;
    private float maxX = 0;
    private float maxY = 0;
    private Dictionary<AstNode, SKPoint> nodePositions;
    private Dictionary<AstNode, NodeSize> nodeSizes;

    // Estructura para almacenar dimensiones de nodo según su tipo
    private struct NodeSize
    {
        public float Width;
        public float Height;
    }

    public AstFlowchartGenerator()
    {
        nodePositions = new Dictionary<AstNode, SKPoint>();
        nodeSizes = new Dictionary<AstNode, NodeSize>();

        // Initialize SkiaSharp text paint and font
        textFont = new SKFont
        {
            Size = 14,
            Typeface = SKTypeface.Default,
            Edging = SKFontEdging.SubpixelAntialias
        };

        textPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            FakeBoldText = true
        };

        // Texto más pequeño para subtipos
        subtextFont = new SKFont
        {
            Size = 10,
            Typeface = SKTypeface.Default,
            Edging = SKFontEdging.SubpixelAntialias
        };

        subtextPaint = new SKPaint
        {
            Color = SKColors.DarkSlateGray,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };
    }

    // Check if two nodes are colliding
    private bool AreNodesColliding(AstNode node1, AstNode node2)
    {
        if (!nodePositions.TryGetValue(node1, out SKPoint pos1) ||
            !nodePositions.TryGetValue(node2, out SKPoint pos2))
            return false;

        float node1Width = GetNodeWidth(node1);
        float node1Height = GetNodeHeight(node1);
        float node2Width = GetNodeWidth(node2);
        float node2Height = GetNodeHeight(node2);

        // Add padding to ensure enough separation between nodes
        float padding = 20;

        // Simplified collision detection based on node type
        string type1 = GetNodeType(node1);
        string type2 = GetNodeType(node2);

        // For circles, use radius-based collision detection
        if (type1 == "Circle" && type2 == "Circle")
        {
            float radius1 = node1Width / 2;
            float radius2 = node2Width / 2;
            float distance = MathF.Sqrt(MathF.Pow(pos2.X - pos1.X, 2) + MathF.Pow(pos2.Y - pos1.Y, 2));
            return distance < (radius1 + radius2 + padding);
        }

        // For rectangles, use AABB collision detection
        if (type1 == "Rectangle" && type2 == "Rectangle")
        {
            return !(pos1.X + node1Width / 2 + padding < pos2.X - node2Width / 2 ||
                     pos1.X - node1Width / 2 - padding > pos2.X + node2Width / 2 ||
                     pos1.Y + node1Height / 2 + padding < pos2.Y - node2Height / 2 ||
                     pos1.Y - node1Height / 2 - padding > pos2.Y + node2Height / 2);
        }

        // For diamonds, use slightly larger bounding box
        if (type1 == "Diamond" || type2 == "Diamond")
        {
            float diamondPadding = padding * 1.5f; // Diamonds need more space
            float width1 = type1 == "Diamond" ? node1Width * 1.2f : node1Width;
            float height1 = type1 == "Diamond" ? node1Height * 1.2f : node1Height;
            float width2 = type2 == "Diamond" ? node2Width * 1.2f : node2Width;
            float height2 = type2 == "Diamond" ? node2Height * 1.2f : node2Height;

            return !(pos1.X + width1 / 2 + diamondPadding < pos2.X - width2 / 2 ||
                     pos1.X - width1 / 2 - diamondPadding > pos2.X + width2 / 2 ||
                     pos1.Y + height1 / 2 + diamondPadding < pos2.Y - height2 / 2 ||
                     pos1.Y - height1 / 2 - diamondPadding > pos2.Y + height2 / 2);
        }

        // Mixed shape collision (simplified to rectangular bounds with padding)
        return !(pos1.X + node1Width / 2 + padding < pos2.X - node2Width / 2 ||
                 pos1.X - node1Width / 2 - padding > pos2.X + node2Width / 2 ||
                 pos1.Y + node1Height / 2 + padding < pos2.Y - node2Height / 2 ||
                 pos1.Y - node1Height / 2 - padding > pos2.Y + node2Height / 2);
    }

    // Reposition colliding nodes
    private void ResolveCollisions(List<AstNode> nodes)
    {
        bool collisionsExist = true;
        int maxIterations = 50; // Prevent infinite loops
        int iteration = 0;

        while (collisionsExist && iteration < maxIterations)
        {
            collisionsExist = false;
            iteration++;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    if (AreNodesColliding(nodes[i], nodes[j]))
                    {
                        collisionsExist = true;

                        // Get current positions
                        SKPoint pos1 = nodePositions[nodes[i]];
                        SKPoint pos2 = nodePositions[nodes[j]];

                        // Calculate vector between nodes
                        float dx = pos2.X - pos1.X;
                        float dy = pos2.Y - pos1.Y;

                        // If nodes are directly on top of each other, add a small offset
                        if (Math.Abs(dx) < 0.001f && Math.Abs(dy) < 0.001f)
                        {
                            dx = 1.0f;
                            dy = 1.0f;
                        }

                        // Normalize the vector
                        float distance = MathF.Sqrt(dx * dx + dy * dy);
                        dx /= distance;
                        dy /= distance;

                        // Calculate minimum distance needed between nodes
                        float minDistance = (GetNodeWidth(nodes[i]) + GetNodeWidth(nodes[j])) / 2 + 40; // Extra padding

                        // Move both nodes apart (weighted by their relative sizes)
                        float weight1 = GetNodeWidth(nodes[i]) / (GetNodeWidth(nodes[i]) + GetNodeWidth(nodes[j]));
                        float weight2 = 1 - weight1;

                        // Calculate the move distance
                        float moveDistance = minDistance - distance;
                        if (moveDistance <= 0) continue;

                        // Update positions
                        nodePositions[nodes[i]] = new SKPoint(
                            pos1.X - dx * moveDistance * weight2,
                            pos1.Y - dy * moveDistance * weight2
                        );

                        nodePositions[nodes[j]] = new SKPoint(
                            pos2.X + dx * moveDistance * weight1,
                            pos2.Y + dy * moveDistance * weight1
                        );

                        // Update bounds after repositioning
                        UpdateBounds(nodes[i], nodePositions[nodes[i]].X, nodePositions[nodes[i]].Y);
                        UpdateBounds(nodes[j], nodePositions[nodes[j]].X, nodePositions[nodes[j]].Y);
                    }
                }
            }
        }
    }

    // Modified CalculateNodePositions method to arrange primary nodes horizontally
    private void CalculateNodePositions(StatementSequenceNode rootNode)
    {
        // Initialize the values
        minX = float.MaxValue;
        minY = float.MaxValue;
        maxX = float.MinValue;
        maxY = float.MinValue;

        // Position the root node
        float rootX = horizontalSpacing;
        float rootY = verticalSpacing;
        nodePositions[rootNode] = new SKPoint(rootX, rootY);

        // Update bounds with the root node
        UpdateBounds(rootNode, rootX, rootY);

        // Create a list to store all nodes for collision detection
        List<AstNode> allNodes = new List<AstNode> { rootNode };

        // If it has subnodes, distribute them horizontally
        if (rootNode.SubNodes.Any())
        {
            // Calculate the total width needed
            float totalNodesWidth = CalculateTotalNodesWidth(rootNode.SubNodes);

            // Center subnodes horizontally relative to parent
            float startX = rootX;
            float currentX = startX;
            float currentY = rootY + GetNodeHeight(rootNode) + verticalSpacing;

            foreach (var subNode in rootNode.SubNodes)
            {
                float nodeWidth = GetNodeWidth(subNode);

                // Place subnodes in a horizontal line
                nodePositions[subNode] = new SKPoint(currentX + nodeWidth / 2, currentY);

                // Update bounds with this subnode
                UpdateBounds(subNode, currentX + nodeWidth / 2, currentY);

                // Move to the next horizontal position
                currentX += nodeWidth + horizontalSpacing;

                // Add to the list of all nodes
                allNodes.Add(subNode);

                // Position sub-subnodes vertically if there are any
                if (subNode is StatementSequenceNode statementSeq && statementSeq.SubNodes.Any())
                {
                    PositionSubnodesRecursively(statementSeq, nodePositions[subNode], 1, allNodes);
                }
            }
        }

        // Detect and resolve collisions between all nodes
        ResolveCollisions(allNodes);
    }


    // Updated recursive positioning method for vertical subnodes arrangement
    private void PositionSubnodesRecursively(StatementSequenceNode node, SKPoint parentPosition, int depth, List<AstNode> allNodes)
    {
        if (!node.SubNodes.Any())
            return;

        float parentX = parentPosition.X;
        float parentY = parentPosition.Y;
        float parentHeight = GetNodeHeight(node);

        // Start vertical positioning
        float currentY = parentY + parentHeight + verticalSpacing;

        // Place nodes vertically aligned with parent
        foreach (var subNode in node.SubNodes)
        {
            // Position subnodes vertically aligned with parent
            nodePositions[subNode] = new SKPoint(parentX, currentY);

            // Update bounds with this subnode
            UpdateBounds(subNode, parentX, currentY);

            // Move to the next vertical position
            currentY += GetNodeHeight(subNode) + verticalSpacing;

            // Add to the list of all nodes for collision detection
            allNodes.Add(subNode);

            // If this is also a sequence, position its subnodes
            if (subNode is StatementSequenceNode statementSeq)
            {
                PositionSubnodesRecursively(statementSeq, nodePositions[subNode], depth + 1, allNodes);
            }
        }
    }

    // Modified GenerateFromAst method to use the collision detection
    public SKBitmap GenerateFromAst(StatementSequenceNode rootNode)
    {
        // Calculate dimensions of the nodes
        CalculateNodeDimensions(rootNode);

        // Calculate positions of nodes
        CalculateNodePositions(rootNode);

        // Calculate the size of the diagram
        SKSizeI imageSize = CalculateImageSize();

        // Create the bitmap with the calculated size
        SKBitmap bitmap = new SKBitmap(imageSize.Width, imageSize.Height);

        // Create the canvas for drawing
        using (canvas = new SKCanvas(bitmap))
        {
            // Clear the canvas
            canvas.Clear(SKColors.White);

            // Draw the flowchart with an offset to center it
            float offsetX = -minX + 80;
            float offsetY = -minY + 80;

            DrawFlowchartWithOffset(rootNode, offsetX, offsetY);
        }

        return bitmap;
    }

    // Nuevo método para calcular dimensiones de cada nodo según su tipo
    private void CalculateNodeDimensions(StatementSequenceNode rootNode)
    {
        CalculateNodeDimension(rootNode);

        foreach (var subNode in rootNode.SubNodes)
        {
            CalculateNodeDimension(subNode);

            if (subNode is StatementSequenceNode statementSeq)
            {
                CalculateNodeDimensions(statementSeq);
            }
        }
    }

    private void CalculateNodeDimension(AstNode node)
    {
        string nodeType = GetNodeType(node);
        float width = nodeWidth;
        float height = nodeHeight;

        // Ajustar tamaño según el tipo de nodo
        if (nodeType == "Diamond")
        {
            // Los diamantes necesitan más espacio
            width = nodeWidth * 1.2f;
            height = nodeHeight * 1.2f;
        }
        else if (nodeType == "Circle")
        {
            // Los círculos pueden ser un poco más grandes
            width = nodeWidth * 1.1f;
            height = nodeWidth * 1.1f; // Mantener aspecto circular
        }

        // Guardar las dimensiones calculadas
        nodeSizes[node] = new NodeSize { Width = width, Height = height };
    }

    // Método para calcular el ancho total que ocuparán los nodos
    private float CalculateTotalNodesWidth(IEnumerable<AstNode> nodes)
    {
        if (!nodes.Any())
            return 0;

        float totalWidth = 0;

        foreach (var node in nodes)
        {
            totalWidth += GetNodeWidth(node);
        }

        // Añadir espaciado entre nodos
        totalWidth += horizontalSpacing * (nodes.Count() - 1);

        return totalWidth;
    }

    private void PositionSubnodesRecursively(StatementSequenceNode node, SKPoint parentPosition, int depth)
    {
        if (!node.SubNodes.Any())
            return;

        float parentX = parentPosition.X;
        float parentY = parentPosition.Y;
        float parentHeight = GetNodeHeight(node);
        float nextY = parentY + parentHeight + verticalSpacing;

        // Calcular ancho total basado en los subnodos
        float totalNodesWidth = CalculateTotalNodesWidth(node.SubNodes);

        // Calcular posición inicial para centrar los subnodos bajo el padre
        float startX = Math.Max(parentX - totalNodesWidth / 2, horizontalSpacing);

        float currentX = startX;
        foreach (var subNode in node.SubNodes)
        {
            float nodeWidth = GetNodeWidth(subNode);

            // Ajustar la posición para centrar el nodo
            nodePositions[subNode] = new SKPoint(currentX + nodeWidth / 2, nextY);

            // Actualizar límites con este subnodo
            UpdateBounds(subNode, currentX + nodeWidth / 2, nextY);

            // Mover a la siguiente posición con espacio adicional
            currentX += nodeWidth + horizontalSpacing;

            // Recursión si es StatementSequenceNode
            if (subNode is StatementSequenceNode statementSeq)
            {
                PositionSubnodesRecursively(statementSeq, nodePositions[subNode], depth + 1);
            }
        }
    }

    // Métodos auxiliares para obtener dimensiones del nodo
    private float GetNodeWidth(AstNode node)
    {
        if (nodeSizes.TryGetValue(node, out NodeSize size))
            return size.Width;
        return nodeWidth;
    }

    private float GetNodeHeight(AstNode node)
    {
        if (nodeSizes.TryGetValue(node, out NodeSize size))
            return size.Height;
        return nodeHeight;
    }

    private void UpdateBounds(AstNode node, float x, float y)
    {
        string nodeType = GetNodeType(node);
        float halfWidth = GetNodeWidth(node) / 2;
        float halfHeight = GetNodeHeight(node) / 2;

        // Añadir un margen adicional
        float margin = 15;

        minX = Math.Min(minX, x - halfWidth - margin);
        minY = Math.Min(minY, y - halfHeight - margin);
        maxX = Math.Max(maxX, x + halfWidth + margin);
        maxY = Math.Max(maxY, y + halfHeight + margin);
    }

    private SKSizeI CalculateImageSize()
    {
        // Añadir margen al tamaño del lienzo
        int margin = 100; 
        int width = (int)(maxX - minX + 2 * margin);
        int height = (int)(maxY - minY + 2 * margin);

        // Asegurar tamaños mínimos
        width = Math.Max(width, 800);  
        height = Math.Max(height, 600); 

        return new SKSizeI(width, height);
    }

    private void DrawFlowchartWithOffset(StatementSequenceNode rootNode, float offsetX, float offsetY)
    {
        // Primero dibujar las conexiones
        DrawEdgesWithOffset(rootNode, offsetX, offsetY);

        // Luego dibujar los nodos
        DrawNodesWithOffset(rootNode, offsetX, offsetY);
    }

    private void DrawNodesWithOffset(StatementSequenceNode rootNode, float offsetX, float offsetY)
    {
        // Initialize paints
        SKPaint nodePaint = new SKPaint
        {
            Color = SKColors.LightBlue,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

        SKPaint borderPaint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        // Dibujar el nodo raíz
        if (nodePositions.TryGetValue(rootNode, out SKPoint rootPos))
        {
            float x = rootPos.X + offsetX;
            float y = rootPos.Y + offsetY;

            // Obtener tipo y dibujar
            string nodeType = GetNodeType(rootNode);
            if (!string.IsNullOrEmpty(nodeType))
            {
                DrawNode(x, y, rootNode, nodeType, nodePaint, borderPaint, "Nodos: " + rootNode.SubNodes?.Count());
            }
        }

        // Dibujar todos los subnodos
        foreach (var subNode in rootNode.SubNodes)
        {
            if (nodePositions.TryGetValue(subNode, out SKPoint pos))
            {
                float x = pos.X + offsetX;
                float y = pos.Y + offsetY;

                // Obtener tipo y dibujar
                string nodeType = GetNodeType(subNode);

                DrawNode(x, y, subNode, nodeType, nodePaint, borderPaint, null);

                // Si es secuencia, recursivamente dibujar sus subnodos
                if (subNode is StatementSequenceNode statementSeq)
                {
                    DrawNodesWithOffset(statementSeq, offsetX, offsetY);
                }
            }
        }
    }

    private void DrawEdgesWithOffset(StatementSequenceNode rootNode, float offsetX, float offsetY)
    {
        // Crear un paint para las líneas
        SKPaint edgePaint = new SKPaint
        {
            Color = SKColors.Gray.WithAlpha(180),
            IsAntialias = true,
            StrokeWidth = 1.5f
        };

        // Dibujar conexiones entre el nodo raíz y sus subnodos
        if (nodePositions.TryGetValue(rootNode, out SKPoint rootPos))
        {
            SKPoint fromPos = new SKPoint(rootPos.X + offsetX, rootPos.Y + offsetY);

            foreach (var subNode in rootNode.SubNodes)
            {
                if (nodePositions.TryGetValue(subNode, out SKPoint subPos))
                {
                    SKPoint toPos = new SKPoint(subPos.X + offsetX, subPos.Y + offsetY);
                    DrawEdge(rootNode, fromPos, subNode, toPos, edgePaint);

                    // Si es secuencia, recursivamente dibujar conexiones
                    if (subNode is StatementSequenceNode statementSeq)
                    {
                        DrawEdgesWithOffset(statementSeq, offsetX, offsetY);
                    }
                }
            }
        }
    }

    private string GetNodeType(AstNode node)
    {
        // Determinar la forma del nodo según su tipo
        var typeName = node.NameAst;

        if (string.IsNullOrEmpty(typeName))
        {
            return "Rectangle";
        }

        typeName = typeName.ToLower();

        if (typeName.Contains("declaracion"))
            return "Diamond";
        else if (typeName.Contains("if") || typeName.Contains("while") || typeName.Contains("for") || typeName.Contains("else") || typeName.Contains("switch"))
            return "Diamond";
        else if (typeName.Contains("funcion") || typeName.Contains("metodo"))
            return "Circle";
        else
            return "Rectangle";
    }

    private void DrawNode(float x, float y, AstNode node, string nodeType, SKPaint nodePaint, SKPaint borderPaint, string subtext)
    {
        // Adaptar el color según el tipo de nodo
        SKColor nodeColor;
        string label = node.NameAst;

        if (string.IsNullOrEmpty(label))
        {
            label = "Sin etiqueta";
        }

        label = label.ToLower();

        if (label.Contains("variable"))
            nodeColor = SKColors.LightBlue;
        else if (label.Contains("if"))
            nodeColor = SKColors.LightPink;
        else if (label.Contains("while"))
            nodeColor = SKColors.LightPink;
        else if (label.Contains("for"))
            nodeColor = SKColors.LightPink;
        else if (label.Contains("else"))
            nodeColor = SKColors.LightPink;
        else if (label.Contains("switch"))
            nodeColor = SKColors.LightPink;
        else if (label.Contains("break"))
            nodeColor = SKColors.LightPink;
        else if (label.Contains("declaración"))
            nodeColor = SKColors.LightSalmon;
        else if (label.Contains("metodo"))
            nodeColor = SKColors.LightYellow;
        else
            nodeColor = SKColors.LightGray;

        SKPaint customNodePaint = new SKPaint() { Color = nodeColor };

        // Obtener dimensiones personalizadas para este nodo
        float width = GetNodeWidth(node);
        float height = GetNodeHeight(node);

        switch (nodeType.ToLower())
        {
            case "rectangle":
                DrawRectangle(x, y, width, height, label, customNodePaint, borderPaint, subtext);
                break;

            case "circle":
                DrawCircle(x, y, width / 2, label, customNodePaint, borderPaint, subtext);
                break;

            case "diamond":
                DrawDiamond(x, y, width, height, label, customNodePaint, borderPaint, subtext);
                break;

            default:
                DrawRectangle(x, y, width, height, label, customNodePaint, borderPaint, subtext);
                break;
        }
    }

    // Método mejorado para medir texto usando SKFont
    private float MeasureTextWidth(string text, SKFont font)
    {
        return font.MeasureText(text);
    }

    // Añade este método para dividir el texto en múltiples líneas
    private List<string> WrapText(string text, float maxWidth, SKFont font)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(text))
            return lines;

        // Si el texto es corto, no hace falta dividirlo
        if (MeasureTextWidth(text, font) <= maxWidth)
        {
            lines.Add(text);
            return lines;
        }

        // Dividir por palabras
        string[] words = text.Split(' ');
        string currentLine = words[0];

        for (int i = 1; i < words.Length; i++)
        {
            string word = words[i];
            if (MeasureTextWidth(currentLine + " " + word, font) <= maxWidth)
            {
                currentLine += " " + word;
            }
            else
            {
                lines.Add(currentLine);
                currentLine = word;
            }
        }

        // Añadir la última línea
        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    // Modificar DrawRectangle para texto multilínea
    private void DrawRectangle(float x, float y, float width, float height, string label, SKPaint nodePaint, SKPaint borderPaint, string subtext)
    {
        // Calcular si necesitamos expandir el rectángulo
        List<string> lines = WrapText(label, width - 20, textFont);
        float textHeight = lines.Count * textFont.Size * 1.5f;
        float adjustedHeight = Math.Max(height, textHeight + 20); // 20px de margen

        // Draw the rectangle for the node
        canvas.DrawRoundRect(x - width / 2, y - adjustedHeight / 2, width, adjustedHeight, 10, 10, nodePaint);
        canvas.DrawRoundRect(x - width / 2, y - adjustedHeight / 2, width, adjustedHeight, 10, 10, borderPaint);

        // Dibujar cada línea de texto con método actualizando usando SKFont
        float lineY = y - ((lines.Count - 1) * textFont.Size * 0.75f);
        foreach (string line in lines)
        {
            // Usar DrawText con SKFont y posición correcta
            canvas.DrawText(line, x, lineY, textFont, textPaint);
            lineY += textFont.Size * 1.5f;
        }

        // Dibujar subtexto si existe
        if (!string.IsNullOrEmpty(subtext))
        {
            canvas.DrawText(subtext, x, y + adjustedHeight / 3 + 5, subtextFont, subtextPaint);
        }
    }

    // Modificar DrawCircle para texto multilínea
    private void DrawCircle(float x, float y, float radius, string label, SKPaint nodePaint, SKPaint borderPaint, string subtext)
    {
        // Calcular si necesitamos expandir el círculo
        List<string> lines = WrapText(label, radius * 1.5f, textFont);
        float textHeight = lines.Count * textFont.Size * 1.5f;
        float adjustedRadius = Math.Max(radius, textHeight / 1.6f); // Ajustar para que quepan las líneas

        // Draw the circle for the node
        canvas.DrawCircle(x, y, adjustedRadius, nodePaint);
        canvas.DrawCircle(x, y, adjustedRadius, borderPaint);

        // Dibujar cada línea de texto
        float lineY = y - ((lines.Count - 1) * textFont.Size * 0.75f);
        foreach (string line in lines)
        {
            canvas.DrawText(line, x, lineY, textFont, textPaint);
            lineY += textFont.Size * 1.5f;
        }

        // Dibujar subtexto si existe
        if (!string.IsNullOrEmpty(subtext))
        {
            canvas.DrawText(subtext, x, y + adjustedRadius / 2 + 5, subtextFont, subtextPaint);
        }
    }

    // Modificar DrawDiamond para texto multilínea
    private void DrawDiamond(float x, float y, float width, float height, string label, SKPaint nodePaint, SKPaint borderPaint, string subtext)
    {
        // Calcular si necesitamos expandir el diamante
        List<string> lines = WrapText(label, width * 0.7f, textFont);
        float textHeight = lines.Count * textFont.Size * 1.5f;
        float adjustedHeight = Math.Max(height, textHeight * 1.4f); // Ajustar para que quepan las líneas
        float adjustedWidth = Math.Max(width, adjustedHeight * (width / height)); // Mantener proporción

        // Draw the diamond shape (rotated rectangle)
        SKPath path = new SKPath();
        path.MoveTo(x, y - adjustedHeight / 2);
        path.LineTo(x + adjustedWidth / 2, y);
        path.LineTo(x, y + adjustedHeight / 2);
        path.LineTo(x - adjustedWidth / 2, y);
        path.Close();

        canvas.DrawPath(path, nodePaint);
        canvas.DrawPath(path, borderPaint);

        // Dibujar cada línea de texto
        float lineY = y - ((lines.Count - 1) * textFont.Size * 0.75f);
        foreach (string line in lines)
        {
            canvas.DrawText(line, x, lineY, textFont, textPaint);
            lineY += textFont.Size * 1.5f;
        }

        // Dibujar subtexto si existe
        if (!string.IsNullOrEmpty(subtext))
        {
            canvas.DrawText(subtext, x, y + adjustedHeight / 3 + 5, subtextFont, subtextPaint);
        }
    }

    // Actualizar el método AdjustEdgePoint para que tenga en cuenta los tamaños ajustados
    private SKPoint AdjustEdgePoint(AstNode node, SKPoint point, SKPoint otherPoint)
    {
        string nodeType = GetNodeType(node);
        float width = GetNodeWidth(node);
        float height = GetNodeHeight(node);

        // Para nodos con texto multilínea, podríamos necesitar tamaños ajustados
        // (Este es un enfoque simplificado - idealmente guardaríamos los tamaños ajustados)
        string label = node.NameAst;
        if (!string.IsNullOrEmpty(label))
        {
            List<string> lines = WrapText(label, width * 0.8f, textFont);
            float textHeight = lines.Count * textFont.Size * 1.5f;

            if (nodeType == "Circle")
            {
                float adjustedRadius = Math.Max(width / 2, textHeight / 1.6f);
                width = height = adjustedRadius * 2;
            }
            else if (nodeType == "Diamond")
            {
                float adjustedHeight = Math.Max(height, textHeight * 1.4f);
                float adjustedWidth = Math.Max(width, adjustedHeight * (width / height));
                width = adjustedWidth;
                height = adjustedHeight;
            }
            else // Rectangle
            {
                float adjustedHeight = Math.Max(height, textHeight + 20);
                height = adjustedHeight;
            }
        }

        // Calcular dirección desde este punto al otro
        float dx = otherPoint.X - point.X;
        float dy = otherPoint.Y - point.Y;

        // Normalizar dirección
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 0.0001f)
            return point;

        dx /= length;
        dy /= length;

        // Ajustar según tipo de nodo
        if (nodeType == "Circle")
        {
            float radius = width / 2;
            return new SKPoint(
                point.X + dx * radius,
                point.Y + dy * radius
            );
        }
        else if (nodeType == "Diamond")
        {
            // Para diamante, es más complejo, pero podemos usar esta aproximación
            float distanceX = Math.Abs(dx) * width / 2;
            float distanceY = Math.Abs(dy) * height / 2;
            float distance = Math.Min(distanceX, distanceY);

            return new SKPoint(
                point.X + dx * distance,
                point.Y + dy * distance
            );
        }
        else // Rectangle
        {
            float halfWidth = width / 2;
            float halfHeight = height / 2;

            // Determinar punto de intersección con el rectángulo
            float t1 = Math.Abs(dx) < 0.0001f ? float.MaxValue : ((dx > 0 ? halfWidth : -halfWidth) / dx);
            float t2 = Math.Abs(dy) < 0.0001f ? float.MaxValue : ((dy > 0 ? halfHeight : -halfHeight) / dy);
            float t = Math.Min(t1, t2);

            return new SKPoint(
                point.X + dx * t,
                point.Y + dy * t
            );
        }
    }

    private void DrawEdge(AstNode fromNode, SKPoint from, AstNode toNode, SKPoint to, SKPaint edgePaint)
    {
        // Ajustar puntos de inicio y fin para evitar intersecciones con los nodos
        SKPoint adjustedFrom = AdjustEdgePoint(fromNode, from, to);
        SKPoint adjustedTo = AdjustEdgePoint(toNode, to, from);

        // Draw the edge (line)
        canvas.DrawLine(adjustedFrom, adjustedTo, edgePaint);
        DrawArrow(adjustedFrom, adjustedTo, edgePaint);
    }

    private void DrawArrow(SKPoint from, SKPoint to, SKPaint paint)
    {
        float arrowSize = 10;
        SKPoint direction = new SKPoint(to.X - from.X, to.Y - from.Y);
        float length = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y);

        // Evitar división por cero
        if (length < 0.0001f)
            return;

        direction.X /= length;
        direction.Y /= length;

        // Dibujar puntas de flecha
        SKPoint arrowHead1 = new SKPoint(
            to.X - direction.X * arrowSize - direction.Y * arrowSize,
            to.Y - direction.Y * arrowSize + direction.X * arrowSize
        );

        SKPoint arrowHead2 = new SKPoint(
            to.X - direction.X * arrowSize + direction.Y * arrowSize,
            to.Y - direction.Y * arrowSize - direction.X * arrowSize
        );

        // Dibujar la línea y la flecha
        canvas.DrawLine(from, to, paint);
        canvas.DrawLine(to, arrowHead1, paint);
        canvas.DrawLine(to, arrowHead2, paint);
    }

    // Método para usar directamente desde tu compilador
    public static bool GenerateAstDiagram(StatementSequenceNode ast, string outputPath)
    {
        try
        {
            var generator = new AstFlowchartGenerator();
            SKBitmap bitmap = generator.GenerateFromAst(ast);

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(outputPath))
            {
                data.SaveTo(stream);
            }
            return true;
        }
        catch(Exception ex)
        {
            return false;
        }
    }
}