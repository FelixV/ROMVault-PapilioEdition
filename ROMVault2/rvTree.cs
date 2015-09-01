/******************************************************
 *     ROMVault2 is written by Gordon J.              *
 *     Contact gordon@ROMVault.com                    *
 *     Copyright 2014                                 *
 ******************************************************/

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ROMVault2.RvDB;

namespace ROMVault2
{

    public partial class RvTree : UserControl
    {

       
        public event MouseEventHandler RvSelected;

        private RvDir _lTree;

        private RvDir _lSelected;

        public RvTree()
        {
            InitializeComponent();
        }

        #region "Setup"
        private int _yPos;

        public void Setup(ref RvDir dirTree)
        {
            _lSelected = null;
            _lTree = dirTree;
            SetupInt();
        }

        private void SetupInt()
        {
            _yPos = 0;

            int treeCount = _lTree.ChildCount;

            if (treeCount >= 1)
            {
                for (int i = 0; i < treeCount - 1; i++)
                    SetupTree((RvDir)_lTree.Child(i), "├");

                SetupTree((RvDir)_lTree.Child(treeCount - 1), "└");
            }
            AutoScrollMinSize = new Size(500, _yPos);
            Refresh();
        }

        private void SetupTree(RvDir pTree, string pTreeBranches)
        {
            int nodeDepth = pTreeBranches.Length - 1;

            pTree.Tree.TreeBranches = pTreeBranches;

            pTree.Tree.RTree = new Rectangle(0, _yPos - 8, 1 + nodeDepth * 18, 16);
            pTree.Tree.RExpand = new Rectangle(5 + nodeDepth * 18, _yPos + 4, 9, 9);
            pTree.Tree.RChecked = new Rectangle(20 + nodeDepth * 18, _yPos + 2, 13, 13);
            pTree.Tree.RIcon = new Rectangle(35 + nodeDepth * 18, _yPos, 16, 16);
            pTree.Tree.RText = new Rectangle(51 + nodeDepth * 18, _yPos, 500, 16);

            pTreeBranches = pTreeBranches.Replace("├", "│");
            pTreeBranches = pTreeBranches.Replace("└", " ");

            _yPos = _yPos + 16;

            bool found = false;
            int last = 0;
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvBase t = pTree.Child(i);
                if (t is RvDir)
                    if (((RvDir)t).Tree != null)
                    {
                        found = true;
                        if (pTree.Tree.TreeExpanded)
                            last = i;
                    }
            }
            if (!found)
                pTree.Tree.RExpand = new Rectangle(0, 0, 0, 0);



            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvBase t = pTree.Child(i);
                if (t is RvDir)
                    if (((RvDir)t).Tree != null)
                    {
                        if (pTree.Tree.TreeExpanded)
                        {
                            if (i != last)
                                SetupTree((RvDir)pTree.Child(i), pTreeBranches + "├");
                            else
                                SetupTree((RvDir)pTree.Child(i), pTreeBranches + "└");
                        }
                    }
            }


        }
        #endregion

        #region "Paint"
        private int _hScroll;
        private int _vScroll;
        protected override void OnPaint(PaintEventArgs e)
        {

            Graphics g = e.Graphics;

            _hScroll = HorizontalScroll.Value;
            _vScroll = VerticalScroll.Value;

            Rectangle t = new Rectangle(e.ClipRectangle.Left + _hScroll, e.ClipRectangle.Top + _vScroll, e.ClipRectangle.Width, e.ClipRectangle.Height);

            g.FillRectangle(Brushes.White, e.ClipRectangle);

            if (_lTree != null)
                for (int i = 0; i < _lTree.ChildCount; i++)
                {
                    RvBase tBase = _lTree.Child(i);
                    if (tBase is RvDir)
                    {
                        RvDir tDir = (RvDir)tBase;
                        if (tDir.Tree != null)
                            PaintTree(tDir, g, t);
                    }
                }


        }

        private void PaintTree(RvDir pTree, Graphics g, Rectangle t)
        {
            int y = pTree.Tree.RTree.Top - _vScroll;

            if (pTree.Tree.RTree.IntersectsWith(t))
            {
                Pen p = new Pen(Brushes.Gray, 1) { DashStyle = DashStyle.Dot };

                string lTree = pTree.Tree.TreeBranches;
                for (int j = 0; j < lTree.Length; j++)
                {
                    int x = j * 18 - _hScroll;
                    string cTree = lTree.Substring(j, 1);
                    switch (cTree)
                    {
                        case "│":
                            g.DrawLine(p, x + 9, y, x + 9, y + 16);
                            break;

                        case "├":
                        case "└":
                            g.DrawLine(p, x + 9, y, x + 9, y + 16);
                            g.DrawLine(p, x + 9, y + 16, x + 27, y + 16);
                            break;
                    }
                }
            }

            if (!pTree.Tree.RExpand.IsEmpty)
                if (pTree.Tree.RExpand.IntersectsWith(t))
                {
                    g.DrawImage(pTree.Tree.TreeExpanded ? rvImages.ExpandBoxMinus : rvImages.ExpandBoxPlus, RSub(pTree.Tree.RExpand, _hScroll, _vScroll));
                }


            if (pTree.Tree.RChecked.IntersectsWith(t))
            {
                switch (pTree.Tree.Checked)
                {
                    case RvTreeRow.TreeSelect.Disabled:
                        g.DrawImage(rvImages.TickBoxDisabled, RSub(pTree.Tree.RChecked, _hScroll, _vScroll));
                        break;
                    case RvTreeRow.TreeSelect.UnSelected:
                        g.DrawImage(rvImages.TickBoxUnTicked, RSub(pTree.Tree.RChecked, _hScroll, _vScroll));
                        break;
                    case RvTreeRow.TreeSelect.Selected:
                        g.DrawImage(rvImages.TickBoxTicked, RSub(pTree.Tree.RChecked, _hScroll, _vScroll));
                        break;
                }
            }

            if (pTree.Tree.RIcon.IntersectsWith(t))
            {
                int icon = 2;
                if (pTree.DirStatus.HasInUnsorted())
                {
                    icon = 4;
                }
                else if (!pTree.DirStatus.HasCorrect())
                {
                    icon = 1;
                }
                else if (!pTree.DirStatus.HasMissing())
                {
                    icon = 3;
                }



                Bitmap bm;
                if (pTree.Dat == null && pTree.DirDatCount != 1) // Directory above DAT's in Tree
                    bm = rvImages.GetBitmap("DirectoryTree" + icon);
                else if (pTree.Dat == null && pTree.DirDatCount == 1) // Directory that contains DAT's
                    bm = rvImages.GetBitmap("Tree" + icon);
                else if (pTree.Dat != null && pTree.DirDatCount == 0) // Directories made by a DAT
                    bm = rvImages.GetBitmap("Tree" + icon);
                else
                {
                    ReportError.SendAndShow("Unknown Tree settings in DisplayTree.");
                    bm = null;
                }

                if (bm != null)
                {
                    g.DrawImage(bm, RSub(pTree.Tree.RIcon, _hScroll, _vScroll));
                }
            }



            Rectangle recBackGround = new Rectangle(pTree.Tree.RText.X, pTree.Tree.RText.Y, Width - pTree.Tree.RText.X + _hScroll, pTree.Tree.RText.Height);

            if (recBackGround.IntersectsWith(t))
            {
                string thistxt;

                if (pTree.Dat == null && pTree.DirDatCount != 1) // Directory above DAT's in Tree
                    thistxt = pTree.Name;
                else if (pTree.Dat == null && pTree.DirDatCount == 1) // Directory that contains DAT's
                    thistxt = pTree.Name + ": " + pTree.DirDat(0).GetData(RvDat.DatData.Description) + " ( Have:" + pTree.DirStatus.CountCorrect() + " \\ Missing: " + pTree.DirStatus.CountMissing() + " )";

                // pTree.Parent.DirDatCount>1: This should probably be a test like parent contains Dat 
                else if (pTree.Dat != null && pTree.Dat.AutoAddDirectory && pTree.Parent.DirDatCount > 1)
                    thistxt = pTree.Name + ": " + pTree.Dat.GetData(RvDat.DatData.Description) + " ( Have:" + pTree.DirStatus.CountCorrect() + " \\ Missing: " + pTree.DirStatus.CountMissing() + " )";
                else if (pTree.Dat != null && pTree.DirDatCount == 0) // Directories made by a DAT
                    thistxt = pTree.Name + " ( Have:" + pTree.DirStatus.CountCorrect() + " \\ Missing: " + pTree.DirStatus.CountMissing() + " )";
                else
                {
                    ReportError.SendAndShow("Unknown Tree settings in DisplayTree.");
                    thistxt = "";
                }


                if (_lSelected != null && pTree.TreeFullName == _lSelected.TreeFullName)
                {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(51, 153, 255)), RSub(recBackGround, _hScroll, _vScroll));
                    g.DrawString(thistxt, new Font("Microsoft Sans Serif", 8), Brushes.White, pTree.Tree.RText.Left - _hScroll, pTree.Tree.RText.Top + 1 - _vScroll);
                }
                else
                {
                    g.DrawString(thistxt, new Font("Microsoft Sans Serif", 8), Brushes.Black, pTree.Tree.RText.Left - _hScroll, pTree.Tree.RText.Top + 1 - _vScroll);
                }
            }

            if (pTree.Tree.TreeExpanded)
                for (int i = 0; i < pTree.ChildCount; i++)
                {
                    RvBase tBase = pTree.Child(i);
                    if (tBase is RvDir)
                    {
                        RvDir tDir = (RvDir)tBase;
                        if (tDir.Tree != null)
                            PaintTree(tDir, g, t);
                    }
                }

        }


        private static Rectangle RSub(Rectangle r, int h, int v)
        {
            Rectangle ret = new Rectangle(r.Left - h, r.Top - v, r.Width, r.Height);
            return ret;
        }
        #endregion

        #region"Mouse Events"
        private bool _mousehit;
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            int x = mevent.X + HorizontalScroll.Value;
            int y = mevent.Y + VerticalScroll.Value;

            if (_lTree != null)
                for (int i = 0; i < _lTree.ChildCount; i++)
                {
                    RvDir tDir = (RvDir)_lTree.Child(i);
                    if (tDir.Tree != null)
                        if (CheckMouseDown(tDir, x, y, mevent))
                            break;
                }

            if (!_mousehit) return;

            SetupInt();
            base.OnMouseDown(mevent);
        }

        public void SetSelected(RvDir selected)
        {
            bool found = false;

            RvDir t = selected;
            while (t != null)
            {
                if (t.Tree != null)
                {
                    if (!found)
                    {
                        _lSelected = t;
                        found = true;
                    }
                    else
                        t.Tree.TreeExpanded = true;
                }
                t = t.Parent;
            }
            SetupInt();
        }

        public RvDir GetSelected()
        {
            return _lSelected;
        }

        private bool CheckMouseDown(RvDir pTree, int x, int y, MouseEventArgs mevent)
        {
            if (pTree.Tree.RChecked.Contains(x, y))
            {
                if (pTree.Tree.Checked == RvTreeRow.TreeSelect.Disabled)
                    return true;

                _mousehit = true;
                SetChecked(pTree, pTree.Tree.Checked == RvTreeRow.TreeSelect.Selected ? RvTreeRow.TreeSelect.UnSelected : RvTreeRow.TreeSelect.Selected);
                return true;
            }

            if (pTree.Tree.RExpand.Contains(x, y))
            {
                _mousehit = true;
                SetExpanded(pTree, mevent.Button == MouseButtons.Right);
                return true;
            }

            if (pTree.Tree.RText.Contains(x, y))
            {
                _mousehit = true;

                if (RvSelected != null)
                    RvSelected(pTree, mevent);

                _lSelected = pTree;
                return true;
            }

            if (pTree.Tree.TreeExpanded)
                for (int i = 0; i < pTree.ChildCount; i++)
                {
                    RvBase rBase = pTree.Child(i);
                    if (rBase is RvDir)
                    {
                        RvDir rDir = (RvDir)rBase;
                        if (rDir.Tree != null)
                            if (CheckMouseDown(rDir, x, y, mevent))
                                return true;
                    }
                }

            return false;
        }

        private static void SetChecked(RvDir pTree, RvTreeRow.TreeSelect nSelection)
        {
            pTree.Tree.Checked = nSelection;
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvBase b = pTree.Child(i);
                if (b is RvDir)
                {
                    RvDir d = (RvDir)b;
                    if (d.Tree != null)
                    {
                        SetChecked(d, nSelection);
                    }
                }
            }
        }

        private static void SetExpanded(RvDir pTree, bool rightClick)
        {
            if (!rightClick)
            {
                pTree.Tree.TreeExpanded = !pTree.Tree.TreeExpanded;
                return;
            }

            // Find the value of the first child node.
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvBase b = pTree.Child(i);
                if (b is RvDir)
                {
                    RvDir d = (RvDir)b;
                    if (d.Tree != null)
                    {
                        //Recusivly Set All Child Nodes to this value
                        SetExpandedRecurse(pTree, !d.Tree.TreeExpanded);
                        break;
                    }
                }
            }
        }

        private static void SetExpandedRecurse(RvDir pTree, bool expanded)
        {
            for (int i = 0; i < pTree.ChildCount; i++)
            {
                RvBase b = pTree.Child(i);
                if (b is RvDir)
                {
                    RvDir d = (RvDir)b;
                    if (d.Tree != null)
                    {
                        d.Tree.TreeExpanded = expanded;
                        SetExpandedRecurse(d, expanded);
                    }
                }
            }
        }
        #endregion

        public RvBase Selected
        {
            get
            {
                return _lSelected;
            }
        }
    }

}
