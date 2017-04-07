using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

/*
 * 
 * Form that only holds a PictureBox drawArea for drawing a naive graph implementation.
 * Listeners:
 * * s: create a sample graph
 * * c: clears the current graph
 * * left double click on empty space: create a vertex
 * * left double click on vertex: markes vertex
 * ** double click on other vertex creates edge between the vertices
 * ** p: calculates a predecessor array based on a bfs originating from the marked vertex
 * ** t: if no predecessor array exists, nothing will happen
 * *** draws a shortest path from the marked vertex to the vertex the predecessor array is based on (if possible)
 * ** right click: removes mark
 * * right click on vertex: remove vertex and all edges that hold this vertex
 * * left click drag on vertex: move this vertex
 */
namespace GraphDrawer
{
    public partial class DrawGraphForm : Form
    {
        private Graph graph;
        private List<int> path;

        private Graphics graphics;
        private Image doubleBufferImage;
        private Graphics doubleBufferGraphics;

        // ui features
        private Vertex movingVertex = null;
        private Vertex drawingEdgeVertex = null;
        private int mouseMoveX = 0;
        private int mouseMoveY = 0;

        // drawing materials
        private Pen blackPen = new Pen(Color.Black);
        private Pen redPen = new Pen(Color.Red, 5.0f);
        private Brush fillVertexWhiteBrush = new SolidBrush(Color.White);
        private Brush fillVertexRedBrush = new SolidBrush(Color.Red);
        private Brush nameBrush = new SolidBrush(Color.Gray);
        private Font nameFont = new Font("Arial", 12);

        public DrawGraphForm()
        {
            // build gui
            InitializeComponent();

            // init empty graph
            graph = new Graph();

            // init graphics for drawing
            graphics = drawingArea.CreateGraphics();
            doubleBufferImage = new Bitmap(drawingArea.Width, drawingArea.Height);
            doubleBufferGraphics = Graphics.FromImage(doubleBufferImage);

            // init event handler
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(DrawGraphForm_KeyPress);
            this.Resize += new EventHandler(DrawGraphForm_Resize);
            drawingArea.MouseDown += new MouseEventHandler(drawingArea_MouseDown);
            drawingArea.MouseMove += new MouseEventHandler(drawingArea_MouseMove);
            drawingArea.MouseUp += new MouseEventHandler(drawingArea_MouseUp);
            drawingArea.MouseDoubleClick += new MouseEventHandler(drawingArea_MouseDoubleClick);
        }

        private void drawingArea_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // add vertex or draw edge from vertex A to vertex B
            if (e.Button == MouseButtons.Left)
            {
                Vertex v = getVertexForCoordOrNull(e);
                if (v != null)
                {
                    if (drawingEdgeVertex == null)
                    {
                        mouseMoveX = e.X;
                        mouseMoveY = e.Y;
                        drawingEdgeVertex = v;
                    }
                    else
                    {
                        graph.addEdge(new Edge(drawingEdgeVertex, v));
                        path = null;
                        drawingEdgeVertex = null;
                    }
                }
                else
                {
                    graph.addVertex(new Vertex(1.0f * e.X / drawingArea.Width, 1.0f * e.Y / drawingArea.Height, ""));
                    path = null;
                }
                drawGraph();
            }
        }

        private Vertex getVertexForCoordOrNull(MouseEventArgs e)
        {
            float projX = 1.0f * e.X / drawingArea.Width;
            float projY = 1.0f * e.Y / drawingArea.Height;
            float minWH = Math.Min(drawingArea.Width, drawingArea.Height);
            Vertex v = graph.getVertexByCoord(projX, projY, minWH / drawingArea.Width, minWH / drawingArea.Height);
            return v;
        }

        private void drawingArea_MouseUp(object sender, MouseEventArgs e)
        {
            // stop moving vertex
            movingVertex = null;
        }

        private void drawingArea_MouseMove(object sender, MouseEventArgs e)
        {
            // move vertex
            if (movingVertex != null)
            {
                if (e.X > 0 && e.X < drawingArea.Width)
                {
                    movingVertex.x = 1.0f * e.X / drawingArea.Width;
                }
                if (e.Y > 0 && e.Y < drawingArea.Height)
                {
                    movingVertex.y = 1.0f * e.Y / drawingArea.Height;
                }
                drawGraph();
            }

            // show help line for edge drawing
            if (drawingEdgeVertex != null)
            {
                mouseMoveX = e.X;
                mouseMoveY = e.Y;
                drawGraph();
            }
        }

        private void drawingArea_MouseDown(object sender, MouseEventArgs e)
        {
            // left: select vertex for moving
            if (e.Button == MouseButtons.Left)
            {
                Vertex v = getVertexForCoordOrNull(e);
                if (v != null)
                {
                    movingVertex = v;
                }
                drawGraph();
            }
            // right: stop drawing edge or remove vertex
            else if (e.Button == MouseButtons.Right)
            {
                if (drawingEdgeVertex != null)
                {
                    drawingEdgeVertex = null;
                }
                else
                {
                    Vertex v = getVertexForCoordOrNull(e);
                    if (v != null)
                    {
                        graph.remove(v);
                        path = null;
                    }
                }
                drawGraph();
            }
        }

        private void DrawGraphForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case 's':
                    graph = Graph.generateSampleGraph();
                    drawGraph();
                    break;
                case 'c':
                    graph = new Graph();
                    drawGraph();
                    break;
                case 'p':
                    if (drawingEdgeVertex != null)
                    {
                        graph.createPredecessorArray(drawingEdgeVertex);
                        drawingEdgeVertex = null;
                        drawGraph();
                    }
                    break;
                case 't':
                    if (drawingEdgeVertex != null)
                    {
                        path = graph.createShortestPath(drawingEdgeVertex);
                        if (path == null)
                        {
                            Console.WriteLine("Predecessors not calculated!");
                        }
                        else if (path.Count == 0)
                        {
                            Console.WriteLine("Vertex not reachable!");
                        }
                        else
                        {
                            drawingEdgeVertex = null;
                            drawGraph();
                        }
                    }
                    break;
            }
        }

        private void DrawGraphForm_Resize(object sender, System.EventArgs e)
        {
            doubleBufferImage.Dispose();
            doubleBufferGraphics.Dispose();
            graphics = drawingArea.CreateGraphics();
            doubleBufferImage = new Bitmap(drawingArea.Width, drawingArea.Height);
            doubleBufferGraphics = Graphics.FromImage(doubleBufferImage);
            drawGraph();
        }

        private void drawGraph()
        {
            // drawingArea not visible
            if (drawingArea.Width <= 0 || drawingArea.Height <= 0)
            {
                return;
            }

            // initialize double buffer
            doubleBufferGraphics.FillRectangle(new SolidBrush(Color.White), 0, 0, drawingArea.Width, drawingArea.Height);

            // draw graph - note: draw edges before vertices or lines will overlap ellipses
            drawEdges(doubleBufferGraphics);
            drawPath(doubleBufferGraphics);
            drawVertices(doubleBufferGraphics);
            drawHelpLineForEdge(doubleBufferGraphics);

            // draw double buffer
            graphics.DrawImage(doubleBufferImage, 0, 0);
        }

        private void drawPath(Graphics doubleBufferGraphics)
        {
            if (path != null)
            {
                for (int pathIndex = 0; pathIndex < path.Count - 1; pathIndex++)
                {
                    Vertex vertex1 = graph.getVertex(path[pathIndex]);
                    Vertex vertex2 = graph.getVertex(path[pathIndex + 1]);
                    float v1x = vertex1.x * drawingArea.Width;
                    float v1y = vertex1.y * drawingArea.Height;
                    float v2x = vertex2.x * drawingArea.Width;
                    float v2y = vertex2.y * drawingArea.Height;
                    doubleBufferGraphics.DrawLine(redPen, v1x, v1y, v2x, v2y);
                }
            }
        }

        private void drawHelpLineForEdge(Graphics doubleBufferGraphics)
        {
            if (drawingEdgeVertex != null)
            {
                float v1x = drawingEdgeVertex.x * drawingArea.Width;
                float v1y = drawingEdgeVertex.y * drawingArea.Height;
                doubleBufferGraphics.DrawLine(blackPen, v1x, v1y, mouseMoveX, mouseMoveY);
            }
        }

        private void drawVertices(Graphics doubleBufferGraphics)
        {
            int radMod = Math.Min(drawingArea.Width, drawingArea.Height);
            for (int i = 0; i < graph.numVertices(); i++)
            {
                Vertex v = graph.getVertex(i);
                float vx = drawingArea.Width * v.x - v.radius * radMod;
                float vy = drawingArea.Height * v.y - v.radius * radMod;
                float dia = 2 * v.radius * radMod;
                if (drawingEdgeVertex != null && drawingEdgeVertex.Equals(v))
                {
                    doubleBufferGraphics.FillEllipse(fillVertexRedBrush, vx, vy, dia, dia);
                }
                else
                {
                    doubleBufferGraphics.FillEllipse(fillVertexWhiteBrush, vx, vy, dia, dia);
                }
                doubleBufferGraphics.DrawEllipse(blackPen, vx, vy, dia, dia);
                doubleBufferGraphics.DrawString(v.name, nameFont, nameBrush, vx + dia, vy - dia / 2);
            }
        }

        private void drawEdges(Graphics doubleBufferGraphics)
        {
            for (int i = 0; i < graph.numEdges(); i++)
            {
                Edge e = graph.getEdge(i);
                float v1x = e.vertex1.x * drawingArea.Width;
                float v1y = e.vertex1.y * drawingArea.Height;
                float v2x = e.vertex2.x * drawingArea.Width;
                float v2y = e.vertex2.y * drawingArea.Height;
                doubleBufferGraphics.DrawLine(blackPen, v1x, v1y, v2x, v2y);
            }
        }
    }
}
