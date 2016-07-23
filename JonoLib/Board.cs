using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Cairo;
using Gtk;
using JLib;
// ODDS AND ENDS TO SORT OUT:
// IMMEDIATE:
// 1A. Deal with both the graph and the plot recalculation time arrangements (track 'Graph.LastGraphRecalcn' and 'Plot.LastPlotRecalcn').
//      It is probably valid to have plot pixel recalculation occurring only when the graph is differently shaped
//      (or when forced programmatically); but I doubt if the plot recalc. time buffer is of any value.
// 1B. If eliminating, though, don't nec. revert properties that set it back to fields, as in many cases
//      the main point of the property is not setting recalculation time but cloning of a referenced array. Esp. true for 'plot', as 'mesh'
//      repeatedly overwrites plot fields with different analogous mesh fields - the overwrites should remain copies, so that 'plot'
//      code changes in the future can't stuff up a Mesh object's fields.
// 1C. Whatever recalcn. times are preserved, make sure that they actually DO prevent recalculation where it is not needed.

// LATER:
// 1. Allow for multiple graphs per board. One graph would always be in focus, whether shown by a margin colour or a slightly different
//     bgd colour or by a number in the main window's title. (Last the best, as it is not visually intrusive e.g. in a snapshot).
//    There is already a method Board.GetFocussedGraph(.) based on this.

/*
BOARDS do not have a public constructor and are not accessible to calling code; instead there are STATIC methods of class Board to handle all
 eventualities. A board is created using static method Board.Create(.), and is destroyed using static method Board.KillBoards(.). An internal
 record of all running boards is kept in private dictionary Board.BoardsRunning, which holds the ONLY NONLOCAL POINTER to the board. At
 creation time a board is added to this dictionary, and at destruction time it is removed from there. LOCAL POINTERS to boards only exist
 inside methods of class Board.

ART OBJECTS belong to classes derived from class Art. That class itself does not have instances (though it is not abstract); instead objects
 derive from its child classes, class Graph and class Plot (and its subclasses).

GRAPH class objects are bound to their board; they never exist as orphaned objects. Currently only one graph is allowed per board, but this
 can easily be changed in a revision. Graph objects have fields containing their coordinates within the board's drawing area as if they
 filled that area; it is envisaged that when we add provision for multiple graphs, the Draw method would divide these coordinates by e.g. 2, 3,
 Graphs can only be created and destroyed via static methods (which assign them to their board), but their pointers can be accessed by user
 code, who may alter fields and properties. The user can and should dereference pointers after using them, but a graph will only be eniolated
 when its owning board is eniolated.

PLOT, PLOTMESH class objects are NOT automatically bound to a graph or board. The user can freely create them and destroy them; in fact he
 has the awesome responsibility of doing so, as no local method does so. However if he wants his plot to be DRAWN, he has to add it to some
 chosen graph's list ArtExtant. When that graph is destroyed (or the plot removed from it by a static class method), its ArtObjects contents
 are dereferenced, which means that the plot itself will be destroyed unless it is referenced in another Graph's list and/or it is still
 referenced by the user's pointer in external code. The user should ensure that there is no memory leakage from the buildup of plots that are
 obsolete but still alive in user space (e.g. in some animation that is constanty generating new plots).
So realize that in normal circumstances a plot is likely to have pointers in two different locales: (a) in holding Graphs, and (b) in the
 user's program structures. It will only cease to occupy memory when both locales have been cleared of pointers to it.

DATA STORAGE: Board pointers persist only in Board.BoardsRunning. Graph pointers persist automatically in a board instance's GraphsExtant,
 but may also exist in user space. Plot (and derived classes) instance pointers persist in user space first, and also in any graphs to which
 the user may have assigned them (the graph's repository being in its PlotsExtant list).

ID NUMBERS: Board IDs start from 1;  Graph IDs start from 10001;  PlotIDs start from 100001. As long as there are not more than 10000 boards
 or 90000 graphs no problems are anticipated. Boards are assigned their number by the Board constructor. Both Graphs and Plots have a common
 ancestor, class Art, which in its base constructor assigns a prima facie ID no, starting at 1; to that, the Graph constructor adds 10000
 and the Plot constructor adds 100000.
These offset values are stored as Board public static int fields: BoardIDBase = 0, GraphIDBase = 10000, PlotIDBase = 100000.
*/

namespace JLib
{
/// <summary>
/// <para>Fields: int BoardID, int GraphID, int Button, uint Time (as returned by Gdk event handler), long MSec ( = msec since 1 AD;
///  from unit JGeneral.cs), doubles X and Y (user coords), doubles pX and pY (pixel coords).</para>
/// <para>Button values: Left = 1, right = 3. For exotic mice, middle = 2. Humbler mice can simulate a middle button by clicking left
///  and right buttons together).</para>
/// <para>Clicks are recorded anywhere in the drawing area, but not above (LblHeader) or below (TVBelow) or on the form's margin.</para>
/// </summary>
  public struct MouseRec
  { public int BoardID, GraphID, Button;
    public uint Time;
    public long MSec;
    public double X, Y, pX, pY;
    public MouseRec(int BoardID_, int GraphID_, uint Button_, uint Time_, long MSec_, double userX, double userY, double pixelX, double pixelY)
    { BoardID = BoardID_;  GraphID = GraphID_;  Button = Convert.ToInt32(Button_);
      Time = Time_;  MSec = MSec_;  X = userX;  Y = userY;  pX = pixelX; pY = pixelY;
    }
    public override string ToString ()
    { return string.Format (
        "BoardID = {0}; GraphID = {1}; Button = {2}; Time = {3}; MSec = {4}; user coords X = {5}, Y = {6}; pixel coords pX = {7}, pY = {8}",
          BoardID, GraphID, Button, Time, MSec, X, Y, pX, pY);
    }
  }

  public struct Tint
  {
    public byte R, G, B;
    public double r, g, b;

    // CONSTRUCTORS    
    public Tint(byte R_, byte G_, byte B_)
    { R = R_;  G = G_;  B = B_;    double divisr = 1.0/ (double) 0xFF;
      r = (double) R * divisr;   g = (double) G * divisr;   b = (double) B * divisr;
    }
    // Designed for use with Gdk colours, where e.g. ".Red" returns a ushort.
    public Tint(ushort Rr, ushort Gg, ushort Bb)
    { R = Convert.ToByte(Rr / 0x100);
      G = Convert.ToByte(Gg / 0x100);
      B = Convert.ToByte(Bb / 0x100);
      double divisr = 1.0/ (double) 0xFFFF;
      r = (double) Rr * divisr;   g = (double) Gg * divisr;   b = (double) Bb * divisr;
    }
    public Tint(double r_, double g_, double b_)
    { r = r_;  g = g_;  b = b_;    double multr = (double) 0xFF;
      R = Convert.ToByte(r * multr);   G = Convert.ToByte(g * multr);   B = Convert.ToByte(b * multr);
    }
    public Tint (Gdk.Color colour)
    { R = (byte) (colour.Red / 0x100);   G = (byte) (colour.Green / 0x100);  B = (byte) (colour.Blue / 0x100);
      double divisr = 1/ (double) 0xFF;
      r = (double) R * divisr;   g = (double) G * divisr;   b = (double) B * divisr;
    }
    // INSTANCE methods
    public override string ToString ()
    { return string.Format ("Bytes: R = {0}, G = {1}, B = {2};  Doubles: r = {3}, g = {4}, b = {5}", R,G,B, r,g,b); }

    // STATIC methods
    public static Tint CopyOf(Tint tiddle)
    { return new Tint(tiddle.R, tiddle.G, tiddle.B); }

    public static Tint[] CopyOfArray(Tint[] tiddly)
    { if (tiddly == null) return null;
      int len = tiddly.Length;
      Tint[] result = new Tint[len];
      for (int i=0; i < len; i++) result[i] = CopyOf(tiddly[i]);
      return result;
    }

    public static Gdk.Color GDKClr(Tint tinto) { return new Gdk.Color(tinto.R, tinto.G, tinto.B); }
    public static Gdk.Color[] GDKClrArray(Tint[] tints)
    { if (tints == null) return null;
      int len = tints.Length;
      Gdk.Color[] result = new Gdk.Color[len];
      for (int i=0; i < len; i++) result[i] = GDKClr(tints[i]);
      return result;
    }
    public static Tint[] TintArray(Gdk.Color[] colours)
    { if (colours == null) return null;
      int len = colours.Length;
      Tint[] result = new Tint[len];
      for (int i=0; i < len; i++) result[i] = new Tint(colours[i]);
      return result;
    }
  }

  public class Board : Gtk.Window
  {

// ********* STATIC STRUCTURES ************************   //static

  // ============= STATIC FIELDS ====================================
    private static int NextBoardID; // set by the static constructor, then incremented with each new board creation.
    private static Dictionary<int, Board> BoardsRunning; // Created as an empty dictionary by static constructor.
            // When a board is created, it adds itself; as the board dies, its OnDelete event removes itfrom the dic'y.
// *** Reconsider inserting the following later, so that each new graph goes into the position of some old graph, just eniolated.
//    public static int LastBoardWidth = -1; // A board enters a value here at each resizing + just before its destruction.
//                   // The value assigned here is for access with the very first board before any signals that would alter it.
//    public static int LastBoardHeight = -1; // As above.
//    public static int LastHeaderHeight = -1, LastDrawingAreaHeight = -1, LastDescriptionHeight = -1; // Heights of these three components;
                // they are set at creation of any board by the STATIC board creator, and reset at enlargement or closure of any board.
    public static readonly int MaxExtraSubMenus = 20;
    /// <summary>
    /// .X is board no., .Y is menu no. If for a valid board .Y is -1, there has been no click since the last was cleared (or since startup).
    /// NB - calling code should always reset it to (0, -1) after handling it.
    /// </summary>
    public static Duo ExtraSubMenuClicked = new Duo(0, -1);
    public static string ImagePathName = "/"; // Set by each successful use of method FileNameDialog(). No record of filename kept.
    public static int MaxMouseClicks = 5; // The maximum size allowed for the next field...
    protected static List<MouseRec> MouseClicks = new List<MouseRec>(MaxMouseClicks); // ...which is accessed by the property...
      /// <summary>
      /// Retrieves a stack of the last few mouse clicks (up to MaxMouseClicks, which is set to 5 at startup; but you can change static field
      /// MaxMouseClicks). The latest click is in [0]. If there have been no clicks yet, the array will be empty (but not null).
      /// </summary>
      public static MouseRec[] Clicks { get { return MouseClicks.ToArray(); } }
    /// <summary>
    /// At each mouse click on a graph, set to the no. of the graph. Calling code should reset it to 0 after reading it, for it to be useful.
    /// </summary>
    protected static MouseRec LastMouseUnclick = new MouseRec(0, 0, 0, 0, 0, 0, 0, 0, 0); // Set by every release of a mouse button.
      public static MouseRec LastUnclick { get { return LastMouseUnclick; } }
    public static int ClickedGraph = 0;
    public static long AccessKey; // used to limit access across classes in this unit.
    public static int BoardIDBase = 0,  GraphIDBase = 10000,  PlotIDBase = 100000; // Keep both GraphIDBase and PlotIDBase at least this high,
      // so that in MonoMaths arguments which are plot nos. can be distinguished from much smaller args. that are e.g. angles, unicodes.
    /// <summary>
    /// A closing board sets LastKilledBoard to its GraphID. This is intended for e.g. animation loops, where a closed graph should be
    ///  the cue for a loop to be exited. (The loop would have to keep polling this value.) It is not set or reset at any other place in this unit.
    /// </summary>
    public static Duo LastKilledBoard = new Duo(0,0); // When a board closes, the board's ID goes into .X and its FOCUSSED graph's ID --> .Y.
                       // Overwritten (by further closures) but not reset in this unit; calling code would usually do this after reading the field.
    protected static int NoFixedMainMenuItems; // set in constructor to the no. main menu items other than 'Extra'.
      public static int NoFixedMenus { get { return NoFixedMainMenuItems; } }
    protected static int MaxKeyPresses = 4; // see instance field KeyPresses. Must be the max. number of keys that can take part in a single
                                            // keypress (i.e. all the helper keys held down, and the main key that follows).
    protected static int MaxKeyings = 10; // The size of the instance stack Keyings ( = stack of keys / key-combinations).

  // ============= STATIC PROPERTIES ================================
    public static int NoBoardsCreated { get { return NextBoardID-1; } } // They don't nec. all still exist!

  // ======= STATIC CONSTRUCTOR ===================================

    static Board()
    { BoardsRunning = new Dictionary<int, Board>();
      NextBoardID = BoardIDBase + 1; // The first Board instance will have this as its BoardID field.
    }

  // ======= STATIC METHODS ===================================   //static
    /// <summary>
    /// <para>Creates a new board which REQUESTS Gtk to use the supplied dimensions. RETURNED: the board ID. (Even if some argument(s) are
    ///  impossible, some sort of board will be created, and so a valid ID - a positive integer - will always be returned.)</para>
    /// <para>WIDTH RESTRICTIONS: The menu is the problem, as it forces the width to be in the high 200's; so as a rule, don't
    ///  try to set a width less than 300. Apart from that (plus screen size), you always get what you ask for.</para>
    /// <para>HEIGHT RESTRICTIONS: Keep to these rules, and you will have the drawing area exactly what you asked for, and the label
    ///  either so or only a pixel out.</para>
    /// <para> -- (1) 'IncludeDescription' TRUE: You get what you ask for as long as board height is not longer than the
    ///  screen height allows. AND provided you allow at least around 40 pixels over for the Description. In fact, the Description
    ///  will occupy 28 pixels less than what you leave over, due to the menu and borders, so for any sensible use you will have to
    ///  leave enough - say, at least 50 for a single line of text (height 22).</para>
    /// <para> -- (2) IncludeDescription FALSE: As long as you make HeaderHeight and DrawingAreaHeight add up EXACTLY to Height_, you
    ///  get what you asked for, down to at least 100 pixels with two similar-size compartments (I haven't bothered testing it below that).</para>
    /// </summary>
    public static int Create(int Width_, int Height_, int HeaderHeight, int DrawingAreaHeight, bool IncludeDescription)
    { Board baud = new Board(Width_, Height_, HeaderHeight, DrawingAreaHeight, IncludeDescription);
      return baud.boardID;
    }

    public static int CopyOf(int BoardID, bool UseOriginalDimensions)
    { Board copycat;
      int[] sz = SizesOf(BoardID);  if (sz == null) return -1;
      if (UseOriginalDimensions)
      { copycat = new Board(sz[8], sz[9], sz[10], sz[11], true); }
      else
      { copycat = new Board(sz[0], sz[1], sz[3], sz[5], true); }
      return copycat.boardID;
    }

    /// <para>If board not identified, returns NULL. Otherwise elements are as follows:</para>
    /// <para>ACTUAL BOARD dimensions: [0] = width, [1] = height, [2] = LblHeader width, [3] = LblHeader height,
    ///  [4] = drawing area width, [5] = drawing area height, [6] = TVBelow width ( or 0, if no TVBelow exists), [7] = TVBelow height (or 0).</para>
    /// <para>ORIG. REQUESTED BOARD dimensions: [8] = width, [9] = height, [10] = eboxHeader height, [11] = drawing area height,
    ///  [12] = bool: 1 if TVBelow was requested, 0 if not. [13] = top, [14] = left;
    ///  Elements [15] to [19] are currently unused, but may be used in the future. To create minimum disturbance in that event,
    ///  the output array size is 20; this allowing calling methods to pack other stuff (e.g. graph dimensions) on top of these 20,
    ///  without it needing to know about the additions. </para>
    public static int[] SizesOf(int BoardID)
    { int[] result = new int[20];
      Board bud;
      if (!BoardsRunning.TryGetValue(BoardID, out bud)) return null;
      Gdk.Rectangle rio = bud.Allocation;
      result[0] = rio.Width;                 result[1] = rio.Height;
      Gdk.Rectangle brio = bud.LblHeader.Allocation;
      result[2] = brio.Width;                result[3] = brio.Height;
      Gdk.Rectangle trio = bud.Da.Allocation;
      result[4] = trio.Width;                result[5] = trio.Height;
      if (bud.TVBelow != null)
      { Gdk.Rectangle prio = bud.TVBelow.Allocation;
        result[6] = prio.Width;              result[7] = prio.Height;
      }
      result[8]  = bud.OrigReqWidth;         result[9] = bud.OrigReqHeight;
      result[10] = bud.OrigReqHeaderHeight;  result[11] = bud.OrigReqDrawingAreaHeight;
      if (bud.OrigIncludeDescription)  result[12] = 1; else result[12] = 0;
      int xint, yint;
      bud.GetPosition(out xint, out yint);
      result[13] = xint;                     result[14] = yint;
     return result;
    }

    /// <summary>
    /// Available for static methods of Board only.
    /// </summary>
    protected static Board GetBoard(int ID)
    { Board result;
      if (!BoardsRunning.TryGetValue(ID, out result)) return null;
      return result;
    }
    /// <summary>
    /// Specifically for use by the CairoGraphic class.
    /// </summary>
    public static Board GetBoardForExpose(object oodle, int BoardID)
    { if (oodle is CairoGraphic) return GetBoard(BoardID);  else return null;
    }

    /// <summary>
    /// Returns the ID nos. of the Board instances currently in existence. If none, returns an empty array (NOT null).
    /// </summary>
    public static int[] CurrentBoards()
    { int n = BoardsRunning.Count;  if (n == 0) return new int[0];
      List<int> result = new List<int>(n);
      foreach (KeyValuePair<int, Board> kvp in BoardsRunning) result.Add(kvp.Key);
      return result.ToArray();
    }
    /// <summary>
    /// Returns TRUE if the board is present in BoardsRunning.
    /// </summary>
    public static bool Exists(int ID)
    { Board buddy;
      return BoardsRunning.TryGetValue(ID, out buddy);
    }
    /// <summary>
    /// Returns the ID nos. of the board's graphs, or NULL if none (or if board not identified).
    /// </summary>
    public static int[] Graphs(int BoardID)
    { Board boodle = GetBoard(BoardID);     if (boodle == null) return null;
      int cnt = boodle.GraphsExtant.Count;  if (cnt == 0) return null;
      int[] result = new int[cnt];
      for (int i=0; i < cnt; i++) result[i] = boodle.GraphsExtant[i].ArtID;
      return result;
    }
    /// <summary>
    /// Returns the Graph instance, if board and graph identified; otherwise returns null.
    /// If AndRemoveFromBoard is TRUE, also removes it from the board's GraphsExtant list.
    /// </summary>
    public static Graph GetGraph(int BoardID, int GraphID, bool AndRemoveFromBoard)
    { Board boodle = GetBoard(BoardID);     if (boodle == null) return null;
      for (int i=0; i < boodle.GraphsExtant.Count; i++)
      { if (boodle.GraphsExtant[i].ArtID == GraphID)
        { Graph gaffe = boodle.GraphsExtant[i];
          if (AndRemoveFromBoard) boodle.GraphsExtant.RemoveAt(i);
          return gaffe;
        }
      }
      return null;
    }
    /// <summary>
    /// *** Currently returns GraphsExtant[0], if board and graph identified; otherwise returns null.
    /// </summary>
    public static Graph GetFocussedGraph(int BoardID)
    { Board boodle = GetBoard(BoardID);     if (boodle == null) return null;
      if (boodle.GraphsExtant.Count < 1) return null;
      return boodle.GraphsExtant[0];
    }
    /// <summary>
    /// If GraphID found, returns: .X = Board ID, .Y = index of this graph in the board's GraphsExtant, .Z = size of board's GraphsExtant;
    /// and ThisGraph holds a pointer to the graph. If not found, returns (0,0,0), and ThisGraph is NULL.
    /// </summary>
    public static Trio GetBoardOfGraph(int GraphID, out Graph ThisGraph)
    { ThisGraph = null;
      foreach (KeyValuePair<int, Board> kvp in BoardsRunning)
      { List<Graph> graphs = kvp.Value.GraphsExtant;
        for (int i=0; i < graphs.Count; i++)
        { if (graphs[i].ArtID == GraphID)
          { ThisGraph = graphs[i];
            return new Trio (kvp.Key, i, graphs.Count);
          }
        }
      }
      return new Trio(0,0,0);
    }

    /// <summary>
    /// Finds all extant plots in all extant graphs in all extant boards. Each element of the returned list has the form: .X = Board ID,
    /// .Y = Graph ID, .Z = Plot ID. If there are no plots, the list is empty (but not null).
    /// </summary>
    public static List<Trio> ListAllPlotsInAllBoards()
    { List<Trio> result = new List<Trio>();
      Trio tree = new Trio(0,0,0);
      foreach (KeyValuePair<int, Board> kvp in BoardsRunning)
      { tree.X = kvp.Key;
        List<Graph> branch = kvp.Value.GraphsExtant;
        foreach (Graph leaf in branch)
        { tree.Y = leaf.ArtID;
          foreach (Plot pluto in leaf.PlotsExtant)
          { tree.Z = pluto.ArtID;
            Trio treecopy = tree;
            result.Add(treecopy);
          }
        }
      }
      return result;
    }

    /// <summary>
    /// Returns that board, if any, which has its TVBelow component focussed; if no boards or none such, returns NULL.
    /// </summary>
    protected static Board FocussedBoard(int dummy)
    { if (BoardsRunning.Count == 0) return null;
      foreach (Board boodle in BoardsRunning.Values)
      { if (boodle.TVBelow.HasFocus) return boodle; }
      return null;
    }
    /// <summary>
    /// Returns the BoardID of that board, if any, which has its TVBelow component focussed; if none such, or no boards exist, returns NULL.
    /// </summary>
    public static int FocussedBoard()
    { Board buddy = FocussedBoard(0);
      if (buddy == null) return -1;
      return buddy.boardID;
    }

    /// <summary>
    /// Returns 1 if success, 0 if would have been a duplicate so not re-added, -1 if the board cannot be identified,
    /// -2 if the graph has ArtID of 0 or negative, -3 if it is null. Forces a redraw, if success.
    /// </summary>
    public static int AddGraph(int BoardID, Graph graph)
    { Board boodle = GetBoard(BoardID);   if (boodle == null) return -1;
      if (graph == null) return -3;
      // Check for duplication:
      int id = graph.ArtID;  if (id < 1) return -2;
      foreach (Graph graffiti in boodle.GraphsExtant)
      { if (graffiti.ArtID == id) return 0; }
      boodle.GraphsExtant.Add(graph);
      Board.ForceRedraw(boodle, false);
      return 1; // even if it was not added because a duplicate was already there.
    }
    /// <summary>
    /// Returns TRUE if success, FALSE if the board cannot be identified, or if the graph was not represented in GraphsExtant.
    /// If true, then a redraw is forced.
    /// </summary>
    public static bool RemoveGraph(int BoardID, int ArtID_)
    { bool result = false;
      Board boodle = GetBoard(BoardID);   if (boodle == null) return result;
      int find = -1;
      for (int i=0; i < boodle.GraphsExtant.Count; i++) { if (boodle.GraphsExtant[i].ArtID == ArtID_) { find = i; break; } }
      if (find >= 0)
      { boodle.GraphsExtant.RemoveAt(find);
        Board.ForceRedraw(boodle, false);
        result = true;
      }
      return result;
    }


    /// <summary>
    /// Always returns the final visibility of the board. If DoWhat is = 0, no change is made. If positive, visibility is set to TRUE;
    ///   if negative, to FALSE. If BoardID does not exist, returns FALSE (with no indication of its nonexistence).
    /// </summary>
    public static bool VisibilityStatus(int BoardID, int DoWhat)
    { Board biddle = GetBoard(BoardID);   if (biddle == null) return false;
      if (DoWhat > 0) biddle.Visible = true;   else if (DoWhat < 0) biddle.Visible = false;
      return biddle.Visible;
    }

    /// <summary>
    /// Returns current setting of the board's MainSubMenuClicked parameter. If there have been no clicks yet, this will be Quad(false).
    /// After each click, .B set to 1, .S set to the submenu's text after removal of space, .X set to msecs since start of 1 AD; but
    /// field .I is not accessed.
    /// If the board does not exist, returns Quad(false) with .X set to -1.0. (It should otherwise never be negative.)
    /// </summary>
    public static Quad Get_MainSubMenuClicked(int BoardID)
    { Board boodle = GetBoard(BoardID);   if (boodle == null) return new Quad(0, -1.0, false, "");
      return boodle.MainSubMenuClicked;
    }
    /// <summary>
    /// The purpose of this is to allow calling code to reset .B to false, and to do what it likes with .I (not accessed in this unit).
    /// There is nothing to stop user changing .S or .X, but this would seem to be rather pointless.
    /// If the board does not exist, returns false (and does nothing else); otherwise true is returned.
    /// </summary>
    public static bool Set_MainSubMenuClicked(int BoardID, Quad NewSetting)
    { Board boodle = GetBoard(BoardID);   if (boodle == null) return false;
      boodle.MainSubMenuClicked = NewSetting;
      return true;
    }
    /// <summary>
    /// Returns an array of the board IDs of all boards holding this Art work (be it a graph or a plot); or NULL if none. (Never returns
    /// an empty array.) If StopAtFirstFind, the return will either be an array of length 1, [0] = first found owning board, or NULL.
    /// If the ID no. represents a graph, and StopAtFirstFind is false, the search continues through all boards. Normally duplication
    /// of graphs in other boards should be impossible; but this facility is here for the paranoid.
    /// </summary>
    public static int[] OwnersOf(int ArtID_, bool StopAtFirstFind)
    { List<int> ownerBoards = new List<int>();
      if (ArtID_ <= Board.GraphIDBase) return null; // a board ID was supplied.
      bool findGraph = (ArtID_ > Board.GraphIDBase && ArtID_ <= Board.PlotIDBase);
      foreach (KeyValuePair<int, Board> kvp in BoardsRunning)
      { Board bord = kvp.Value;  int bordID = bord.boardID;
        List<Graph> graphs = bord.GraphsExtant;
        foreach (Graph gribble in graphs)
        { if (findGraph) // we are looking for a graph, not a plot
          { if (gribble.ArtID == ArtID_)
            { if (StopAtFirstFind) return new int[] {bordID};
              ownerBoards.Add(bordID);
            }
          }
          else // we are looking for a plot, not a graph:
          { List<int> plotids = gribble.PlotsExtantIDs;
            if (plotids.IndexOf(ArtID_) >= 0)
            { if (StopAtFirstFind) return new int[] {bordID};
              ownerBoards.Add(bordID);
            }
          }
        }
      }
      if (ownerBoards.Count == 0) return null;  else return ownerBoards.ToArray();
    }

    /// <summary>
    /// <para>Kill boards which have IDs as provided. If no board exists for IDs[i], the reference is simply
    ///  ignored and no error is raised.</para>
    /// If the argument is empty or null, ALL boards are killed.
    /// <para>All pointers to plots in each graph of the board's GraphsExtant list will be dereferenced. This will only
    ///  kill these plots if there are no user-code pointers still referencing them.</para>
    /// </summary>
      public static void KillBoards(params int[] IDs)
    { List<int> aiDees =new List<int>();
      if (IDs._NorE())
      { foreach (KeyValuePair<int, Board> kvp in BoardsRunning) aiDees.Add(kvp.Key); }
      else aiDees.AddRange(IDs);
      foreach(int id in aiDees)
      { Board doomed;
        if (BoardsRunning.TryGetValue(id, out doomed))
        { List<Graph> graphs = doomed.GraphsExtant;
          LastKilledBoard.X = doomed.boardID;
          LastKilledBoard.Y = 0;
          foreach (Graph graf in graphs)
          { LastKilledBoard.Y = graf.ArtID; // We only want the ID of the last killed graph (or 0, if there are no graphs).
            graf.PlotsExtant.Clear(); // This will only doom these plots if there are no external references to them.
          }
          graphs.Clear();
          BoardsRunning.Remove(id); // hopefully the last reference to this board, so that...
          doomed.Destroy(); // ... this act will take the reference count of the board to zero. But note that when this is called
                            //  from within the window instance (as is normal), the board will not disappear by virtue of this 'destroy';
                            //  the board has to call its own 'destroy' as well (immediately after the call to KillBoards(.) ).
        }
      }
    }

  public static void TryResize(int BoardID, int newWidth, int newHeight)
  { Board bawd;
    if (!BoardsRunning.TryGetValue(BoardID, out bawd)) return;
    bawd.Resize(newWidth, newHeight);
  }

  /// <summary>
  /// Tries to reposition and to resize the board. Usually successful, but is at the mercy of Gdk and also of the Windows Manager of the op. system.
  /// Returns TRUE normally, FALSE only if there is no board. (Inappropriate args. will not return false, but will simply be reinterpreted by Gdk.)
  /// </summary>
  public static bool TryResituate(int BoardID, int newLeft, int newTop, int newWidth, int newHeight)
  { Board bawd;
    if (!BoardsRunning.TryGetValue(BoardID, out bawd)) return false;
    Gdk.Window wawd = bawd.GdkWindow;
    wawd.MoveResize(newLeft, newTop, newWidth, newHeight);
    return true;
  }
  /// <summary>
  /// Forces redrawing of the board's drawing area (not of the whole board). If making a single change, ShowIntermediates can be FALSE,
  ///  as the change will occur immediately (from the viewer's standpoint). But if automating (a rapid series of changes which the user
  ///  is to watch), ShowIntermediates has to be TRUE, or else you will only see the final state.
  /// </summary>
    public static void ForceRedraw(int BoardID, bool ShowIntermediates)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd)) return;
      ForceRedraw(bawd, ShowIntermediates);
    }
  /// <summary>
  /// Forces redrawing of the board's drawing area (not of the whole board). If making a single change, ShowIntermediates can be FALSE,
  ///  as the change will occur immediately (from the viewer's standpoint). But if automating (a rapid series of changes which the user
  ///  is to watch), ShowIntermediates has to be TRUE, or else you will only see the final state.
  /// </summary>
    public static void ForceRedraw(Board Bod, bool ShowIntermediates)
    { Gdk.Rectangle tangle = Bod.Da.Allocation; // Can't use tangle as is, because it is relative to the CLIENT AREA of the main window,
                                                 // and so has a 'top' offset (menu ht. + LblHeader ht.).
      Gdk.Rectangle newfangle = new Gdk.Rectangle(0, 0, tangle.Width, tangle.Height); // 'newfangle' is now the extent of Da relative to ITSELF.
      Gdk.Window wawd = Bod.Da.GdkWindow;
      wawd.InvalidateRect(newfangle, false); // *** the bool arg. here and in next step involves redrawing children. There aren't any at the moment,
                                             //     but if redrawing goes awry in the future, try making both of these 'true'.
      if (ShowIntermediates)  wawd.ProcessUpdates(false);
      Bod.TVBelow.GrabFocus(); // This enables the calling code's key snooper to recognize which board - if any - is being focussed.
                               // But the cursor is not visible.
    }
    /// <summary>
    /// Returns the board's 'ImageFileName' field value, or "" if the board is not identifiable.
    /// </summary>
    public static string GetImageFilename(int BoardID)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd)) return "";
      return bawd.ImageFileName;
    }
    /// <summary>
    /// Sets the board's 'ImageFileName' unless the board is not identifiable - in which case FALSE is returned. The new name is not tested in any way.
    /// </summary>
    public static bool SetImageFilename(int BoardID, string FileName)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd)) return false;
      bawd.ImageFileName = FileName;  return true;
    }
    // A utility for the next two public methods. Returns NULL if MenuName not valid. For menus other than the SUBmenus of menu Extra,
    // only MenuName[0] is ever accessed (case-insensitive). Submenus of Extra: MenuName is set to "", and ExtraMenuNo is accessed.
    private static Gtk.MenuItem GetMenu(Board bored, string MenuName, int ExtraSubMenuNo)
    { int len = MenuName._Length();
      if (len >= 1) // then MenuName is to be used:
      { char c = Char.ToUpper(MenuName[0]);
        switch (c)
        { case 'F' : return bored.FileMenu;
          case 'Z' : return bored.ZoomMenu;
          case 'V' : return bored.ViewPointMenu;
          case 'S' : return bored.ScalingMenu;
          case 'E' : return bored.ExtraMenu;
          default  : return null;
        }
      }
      // If got here, MenuName is null or empty, so consult ExtraSubMenuNo:
      if (ExtraSubMenuNo < 0 || ExtraSubMenuNo >= MaxExtraSubMenus) return null;
      return bored.Extra_SubMenu[ExtraSubMenuNo];
    }
    /// <summary>
    /// <para>Returns the setting of the menu's .Visible field, if the board and menu were both identified; otherwise always FALSE.</para>
    /// <para>All MAIN menu items can have visibility checked, but the only SUBMENUS which can have visibility checked are those of
    ///  the Extras main menu item.</para>
    /// <para>MenuName: Only use for main menu items (leave empty or null for Extra submenu items). Only the first char. will be accessed
    ///  (case-insensitive); it must be the first letter of the menu name (e.g. 'F' or 'f' for the File menu).</para>
    /// <para>ExtraSubMenuNo: Only accessed if MenuName is null or empty.</para>
    /// </summary>
    public static bool GetMenuStatus(int BoardID, string MenuName, int ExtraSubMenuNo)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) return false;
      Gtk.MenuItem moo = GetMenu(bawd, MenuName, ExtraSubMenuNo);
      if (moo == null) return false;
      return moo.Visible;
    }
    /// <summary>
    /// <para>Sets the menu's .Visible field to ToVisible. RETURNS: TRUE if the board and menu were both identified, otherwise FALSE.</para>
    /// <para>All MAIN menu items can have visibility altered, but the only SUBMENUS which can have visibility altered are those of
    ///  the Extras main menu item.</para>
    /// <para>MenuName: Only use for main menu items (leave empty or null for Extra submenu items). Only the first char. will be accessed
    ///  (case-insensitive); it must be the first letter of the menu name (e.g. 'F' or 'f' for the File menu).</para>
    /// </summary>
    public static bool SetMenuStatus(int BoardID, string MenuName, int ExtraSubMenuNo, bool ToVisible)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) return false;
      Gtk.MenuItem moo = GetMenu(bawd, MenuName, ExtraSubMenuNo);
      if (moo == null) return false;
      moo.Visible = ToVisible;
      return true;
    }
    /// <summary>
    /// Sets visibility of main menu items from left to right, excluding the 'Extra' menu. Shortfall in IsVisible: excess menus unaffected.
    /// Excess in IsVisible: excess array elements ignored.
    /// </summary>
    public static void SetFixedMenuVisibilities(int BoardID, bool[] IsVisible)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) return;
      int len = IsVisible._Length();
      if (len > 0) bawd.FileMenu.Visible      = IsVisible[0];
      if (len > 1) bawd.ZoomMenu.Visible      = IsVisible[1];
      if (len > 2) bawd.ViewPointMenu.Visible = IsVisible[2];
      if (len > 3) bawd.ScalingMenu.Visible   = IsVisible[3];
    }
    /// <summary>
    /// Returns TRUE if board identified; then the OUT args. reflect visibility of Extra menu and of its submenus.
    /// </summary>
    public static bool GetExtraMenuVisibilities(int BoardID, out bool MainVisible, out bool[] SubVisible)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) { MainVisible = false;  SubVisible = null;return false; }
      MainVisible = bawd.ExtraMenu.Visible;
      int len = bawd.Extra_SubTitles.Length; // But the user could have made these any length...
      SubVisible = new bool[len];
      for (int i=0; i < len; i++) SubVisible[i] = bawd.Extra_SubMenu[i].Visible;
      return true;
    }
    /// <summary>
    /// If SubVisible is null or empty, existing submenu visibilities still apply. If it is shorter than the number of submenus,
    /// existing visibilities still apply for those beyond its reach. If longer, excess ignored.
    /// Returns TRUE if board identified.
    /// </summary>
    public static bool SetExtraMenuVisibilities(int BoardID, bool MainVisible, bool[] SubVisible)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) return false;
      bawd.ExtraMenu.Visible = MainVisible;
      int subvis = SubVisible._Length();
      int noSubs = bawd.Extra_SubTitles.Length;
      int maxwell = Math.Min(noSubs, subvis);
      for (int i=0; i < maxwell; i++) bawd.Extra_SubMenu[i].Visible = SubVisible[i];
      return true;
    }
    /// <summary>
    /// Adapt the default menu system where it differs for 2D v. 3D graphing.
    /// </summary>
    public static void SetMenusFor2Dv3D(int BoardID, bool Its3D)
    { Board bawd;
      if (BoardsRunning.TryGetValue(BoardID, out bawd) )
      { // Those true only for 2D:   (All values have to be set every time, as prior settings may exist from a previous use of the same board)
        bawd.Zoom_In_H.Visible  = !Its3D;
        bawd.Zoom_Out_H.Visible = !Its3D;
        bawd.Zoom_In_V.Visible  = !Its3D;
        bawd.Zoom_Out_V.Visible = !Its3D;
        // Those true only for 3D:
        bawd.Aspect_New.Visible = Its3D;
        bawd.Aspect_Original.Visible = Its3D;
      }
    }
    /// <summary>
    /// Gets the existing Extra menu titles; the length of the returned SubMenuTitles will be the number of submenus in use.
    /// If the board is not identified, the out parameters are both null and FALSE is returned.
    /// </summary>
    public static bool GetExtraMenuTitles(int BoardID, out string TopMenuTitle, out string[] SubMenuTitles)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) { TopMenuTitle = null;  SubMenuTitles = null;  return false; }
      TopMenuTitle = bawd.Extra_Title;
      SubMenuTitles = bawd.Extra_SubTitles._Copy();
      return true;
    }
    /// <summary>
    /// <para>We can't divide this rather clumsy method up into simpler ones as it is apparently not possible to change any feature
    ///  of a menu or submenu without recreating the parent menu and its submenu children.</para>
    /// <para>Resets Extra menu and submenu titles as stored in Board fields, and then recreates the menu and submenus using them.</para>
    /// <para>MainMenuTitle: If null or empty (after trimming), the preexisting menu title is unchanged.</para>
    /// <para>SubMenuTitles: If null or empty, the preexisting submenu set is unchanged. Also if any of its elements is empty
    ///  (after trimming), the whole preexisting submenu set is unchanged. Otherwise the number of strings in SubMenuTitles is the new
    ///  number of submenus, and SubMenuTitles holds their new titles.</para>
    /// <para>MainVisible: value is applied to the main menu item.</para>
    /// <para>SubVisible: If null or nonempty, all submenus will be visible. Otherwise sets visibility of submenus. (If too short,
    ///  submenus beyond its length are visible by default; if the arg. array is too long, its excess is ignored.)</para>
    /// <para>RETURNED: TRUE if the board was recognized (even if args. were ignored as above), otherwise FALSE.</para>
    /// </summary>
    public static bool SetExtraMenuTitles(int BoardID, string MainMenuTitle, string[] SubMenuTitles, bool MainVisible, bool[] SubVisible)
    { Board bawd;
      if (!BoardsRunning.TryGetValue(BoardID, out bawd) ) return false;
     // Collect the data without committing it yet to board fields or to the menu system:
      bawd.ExtraMenuTitle = MainMenuTitle; // LHS is a property, which does not set its field if the name is improper.
      bawd.ExtraSubmenuTitles = SubMenuTitles; // LHS is a property, so this is not a pointer assignment. Nothing set, if any RHS element is improper.
      int noSubmenus = bawd.Extra_SubTitles.Length; // this time we use the field covered by the above property.
     // Rebuild the Extra menu system:
      bawd.BuildExtraMenuSystem(false);
     // Set the visibility of menu and submenus:
      bawd.ExtraMenu.Visible = MainVisible;
      int lenSubVisible = SubVisible._Length(),  minnie = Math.Min(noSubmenus, lenSubVisible);
      if (minnie < 0) minnie = 0; // occurs if SubVisible is null.
      for (int i=0; i < minnie; i++) bawd.Extra_SubMenu[i].Visible = SubVisible[i];
      for (int i=minnie; i < noSubmenus; i++) bawd.Extra_SubMenu[i].Visible = true;
      return true;
    }

    /// <summary>
    /// <para>Drastic - it irretrievably turns off all graph menu hot keys. The hot key notifications remain on the menu; maybe one day
    ///  I will find out how to remove them also.</para>
    /// <summary>
    public static void DisconnectAllAccelKeys (int BoardID)
    { Board baud;
      if (!BoardsRunning.TryGetValue(BoardID, out baud)) return;
      baud.RemoveAccelGroup(baud.HotKeys);
    }

    public static Duo GetMousePointer(int BoardID)
    { Board baud;
      if (!BoardsRunning.TryGetValue(BoardID, out baud)) return new Duo(-2,-2);
      int x, y;
      baud.Da.GetPointer(out x, out y);
      return new Duo(x,y);
    }

    /// <summary>
    /// <para>The new keypress is recorded in two stacks, (1) a frequently-cleared stack 'Board_object.KeyPresses',
    ///  which only exists so that combination keypresses can be identified; and (2) a stack for users to reference,
    ///  'Board_object.Keyings', cleared only by the user, in which there is one entry per key combination
    ///  (or single key, if not in combination).</para>
    /// <para>Menu hot-keys also get recorded here, which may be useful if you want to know what menu graph change just occurred,
    ///  or if for some perverse reason you want to undo whatever action was just done by the menu.</para>
    /// </summary>
    public static bool RegisterKeyPress(int BoardID, int KeyValue, int KeyTime)
    {
      Board baud;   if (!BoardsRunning.TryGetValue(BoardID, out baud)) return false;
      Graph graf = GetFocussedGraph(BoardID);  if (graf == null) return false;
      Duo[] keyPresses = baud.KeyPresses;  Duo EMPTY = new Duo(0,0);
      // Place the key press on the bottom of the stack:
      for (int i = MaxKeyPresses-1; i > 0; i--) keyPresses[i] = keyPresses[i-1]; // Shift the stack up one.
      keyPresses[0] = new Duo(KeyValue, KeyTime);
          // NOTE the key sequences: SHIFT is represented by 65505 (left key) and 65506 (right key); CONTROL by 65507
          //  (left) and 65508 (right); ALT by anything from 65511 (left key) to 65514 (right key - if you have one).
          //  65513 and -14 refer to ALT key when SHIFT key is also down, even though in this case the SHIFT key still has its
          //  own separate place in the stack). The order of these keys must not be relevant in the handling routine below,
          //  except in that the letter key (e.g. the 'A' of 'cntrl-shift-A') must be the lowest in the stack.
          //  Complication: Where SHIFT is part of the modifier set, the letter key will be the upper case value (e.g. 65 for 'A'),
          //  but if it is not, the letter key has the lower case value (e.g. 97 for 'A'). The reverse is true if CAPS LOCK is down.
          // Finally, note that all helper keys have values >= 65505, all printable keys have values (much) less than 4096,
          //  and arrow keys are - UP:65362; DN: 65364; LEFT: 65361; RIGHT: 65363.
          // Sadly there is no way to tell whether helper keys were down while the printed letter was keyed or if they were down and then
          //  up before the printed letter key went down. I don't think it matters enough to write extra code for this, beyond
          //  the timing check.
          // METHOD TO CHECK HOW MULTIPLE KEY PRESSES ARE RECORDED ON THE STACK: First remark-out all below, except for the final RETURN.
          //  Then run the program and run user code which generates a graph. With NO break point, enter the keypress complex;
          //  then put a break point below; then enter a simple key like 'A' (to get a stack entry '97'). At the break, examine the stack.
      // Clean out any out-of-date keypresses:
      int ptr = -1, timeLimit = 1000; // 1 second and you have to start again. (If you make it longer, there are problems when the user
                                      // wants to switch quickly from no-helper to helper (e.g. from small mvmt UP to large mvmt UP).
      for (int i=1; i < MaxKeyPresses; i++)
      { if (keyPresses[i].Y - KeyTime > timeLimit) { ptr = i;  break; } }
      if (ptr > 0) { for (int i=ptr; i < MaxKeyPresses; i++) keyPresses[i] = EMPTY; }
      // Is this other than a helper key?
      if (KeyValue < 65505) // Note that KeyValue and keyPresses[0].X are the same value.
      { if (KeyValue >= 97 && KeyValue <= 122) KeyValue -= 32; // Convert a lower case letter to an upper case one.
       // Identify the helpers
        char[] charlady = new char[3];
        for (int i=1; i < MaxKeyPresses; i++)
        { int kp = keyPresses[i].X;
          if (kp == 65505 || kp == 65506) charlady[2] = 's';
          else if (kp == 65507 || kp == 65508) charlady[1] = 'c';
          else if (kp >= 65511 && kp <= 65514) charlady[0] = 'a';
          else break; // Stop searching, if any unrecognized helper keys.
        }
        string helpers = charlady._ToString()._Purge(); // Now valid strings will be one of: "", "a", "c", "s", "ac", "as", "cs" and "acs".
       // Update the stack for the user:
        Strint[] keyings = baud.Keyings;
        for (int j = MaxKeyings-1; j > 0; j--) keyings[j] = keyings[j-1];
        keyings[0] = new Strint(KeyValue, helpers);
      // Clear the private stack whenever KeyValue is not a recognized helper key.
        for (int i=0; i < MaxKeyPresses; i++) keyPresses[i] = EMPTY;
      }
      return true;
    }

    /// <summary>
    /// If the board exists, its Keyings[0] is returned and the stack is pushed down. If the stack is empty,
    ///  the returned Strint is (0, ""). If the board could not be identified, it is (-1, "").
    /// 'NoUnpoppedKeys' returns with the no. non-empty entries still in the stack.
    /// </summary>
    public static Strint PopKeyings(int BoardID, out int NoUnpoppedKeys)
    { NoUnpoppedKeys = 0;
      Board baud;
      if (BoardsRunning.TryGetValue(BoardID, out baud))
      { Strint[] keyings = baud.Keyings;
        Strint result = keyings[0];
        for (int i=0; i < MaxKeyings-1; i++)
        { keyings[i] = keyings[i+1];
          if (keyings[i].I > 0) NoUnpoppedKeys++;  else break;
        }
        keyings[MaxKeyings-1] = new Strint(0,"");
        return result;
      }
      else return new Strint(-1,"");
    }
    //##################################
    public static Duo[] ReadKeyPresses(int BoardID)
    {
      Board baud;   if (!BoardsRunning.TryGetValue(BoardID, out baud)) return null;
      Graph graf = GetFocussedGraph(BoardID);  if (graf == null) return null;
      return baud.KeyPresses;
    }

/// <summary>
/// <para>Applies texts to the Main Window (title), LblHeader (above the drawing surface) and TVBelow (below the drawing surface).</para>
/// <para>To leave any of the three with preexisting values, enter as NULL (rather than an empty string).</para>
/// <para>Whatever the value of Heading, if there is no LblHeader in Baud then it is simply ignored; similarly with
/// Description, if there is no TVBelow.</para>
/// <para>Heading can use Pango text tags, while Description can use JTextView texttags.</para>
/// <para>CursorIsVisible applies to the TextView object below the graph (if it exists; otherwise ignored). If omitted, no change
///  is made to the existing cursor visibility. Otherwise it is set to CursorIsVisible[0].</para>
/// <para>If the board does not exist, simply nothing happens. Nothing in this method raises an error.</para>
/// </summary>
    public static void Texts(int BoardID, string WindowTitle, string Heading, string Description, params bool[] CursorIsVisible)
    { Board baud;
      if (!BoardsRunning.TryGetValue(BoardID, out baud)) return;
      if (WindowTitle != null) baud.Title = WindowTitle;
      if (baud.LblHeader != null && Heading != null)
      { baud.LblHeader.UseMarkup = true; // Not enough just to set .UseMarkUp at creation time.
        baud.LblHeader.Layout.Alignment = Pango.Alignment.Center;// If you weren't using markup, LblHeader.Xalign would work; but never use .Justify.
        baud.LblHeader.LabelProp = Heading;
        baud.LblHeaderPresentedText = Heading;
      }
      if (baud.TVBelow != null)
      { if (CursorIsVisible.Length > 0)  baud.TVBelow.CursorVisible = CursorIsVisible[0];
        if (Description != null)
        { JTV.DisplayMarkUpText(baud.TVBelow, ref Description, "fill");
          baud.TVBelowPresentedText = Description;
        }
      }
    }
/// <summary>
/// <para>Retrieves texts used for the Main Window (title), LblHeader (above the drawing surface) and TVBelow (below the drawing surface).</para>
/// <para>Values are never null, even if the corresponding object does not exist.</para>
/// <para>Whatever the value of Heading, if there is no LblHeader in Baud then it is simply ignored; similarly with
/// Description, if there is no TVBelow.</para>
/// </summary>
    public static bool GetTexts(int BoardID, out string WindowTitle, out string Heading, out string OrigDescription, out string CurrentDescripn)
    { WindowTitle = "";  Heading = "";   OrigDescription = "";   CurrentDescripn = "";
      Board baud;
      if (!BoardsRunning.TryGetValue(BoardID, out baud)) return false;
      WindowTitle = baud.Title;
      Heading = baud.LblHeaderPresentedText;
      OrigDescription = baud.TVBelowPresentedText;
      CurrentDescripn = baud.TVBelow.Buffer.Text;
      return true;
    }

///// <summary>
///// <para>Retrieves texts used for the Main Window (title), LblHeader (above the drawing surface) and TVBelow (below the drawing surface).</para>
///// <para>Values are never null, even if the corresponding object does not exist.</para>
///// <para>Whatever the value of Heading, if there is no LblHeader in Baud then it is simply ignored; similarly with
///// Description, if there is no TVBelow.</para>
///// </summary>
//    public static bool GetTexts(int BoardID, out string WindowTitle, out string Heading, out string Description)
//    { WindowTitle = "";  Heading = "";   Description = "";
//      Board baud;
//      if (!BoardsRunning.TryGetValue(BoardID, out baud)) return false;
//      WindowTitle = baud.Title;
//      Heading = baud.LblHeaderPresentedText;
//      Description = baud.TVBelowPresentedText;
//
//      return true;
//    }


// ********* INSTANCE BUILD ************************   //build

    private  Gtk.VBox vboxBase;
    protected Gtk.MenuBar MainMenu;
    protected Gtk.Menu FileSubMenus;
      protected Gtk.MenuItem FileMenu;
      protected Gtk.MenuItem File_SaveImage;
      protected Gtk.MenuItem File_SaveImageAs;
      protected Gtk.MenuItem File_Exit;
    protected Gtk.Menu ZoomSubMenus;
      protected Gtk.MenuItem ZoomMenu;
      protected Gtk.MenuItem Zoom_In;
      protected Gtk.MenuItem Zoom_Out;
      protected Gtk.MenuItem Zoom_In_H;
      protected Gtk.MenuItem Zoom_Out_H;
      protected Gtk.MenuItem Zoom_In_V;
      protected Gtk.MenuItem Zoom_Out_V;
    protected Gtk.Menu ViewPointSubMenus;
      protected Gtk.MenuItem ViewPointMenu;
      protected Gtk.MenuItem ViewPoint_SmallLeft;
      protected Gtk.MenuItem ViewPoint_SmallRight;
      protected Gtk.MenuItem ViewPoint_SmallUp;
      protected Gtk.MenuItem ViewPoint_SmallDown;
      protected Gtk.MenuItem ViewPoint_BigLeft;
      protected Gtk.MenuItem ViewPoint_BigRight;
      protected Gtk.MenuItem ViewPoint_BigUp;
      protected Gtk.MenuItem ViewPoint_BigDown;
    protected Gtk.Menu ScalingSubMenus;
      protected Gtk.MenuItem ScalingMenu;
      protected Gtk.MenuItem Scaling_New;
      protected Gtk.MenuItem Scaling_Original;
      protected Gtk.MenuItem Aspect_New;
      protected Gtk.MenuItem Aspect_Original;
    protected Gtk.Menu ExtraSubMenus;
      protected Gtk.MenuItem ExtraMenu;
      protected Gtk.MenuItem[] Extra_SubMenu = new Gtk.MenuItem[MaxExtraSubMenus];
    protected string Extra_Title = "Extra";
      public string ExtraMenuTitle // Not 'public' for public access, as the public can't get at a board; fed to by static methods below.
      { get { return Extra_Title; }
        set { if (value != null) { string ss = value.Trim(); if (ss != "") Extra_Title = ss; } }
      }
    protected string[] Extra_SubTitles = new string[MaxExtraSubMenus]; // Filled in the constructor (with simple numerals)
      public string[] ExtraSubmenuTitles // Must have length at least 1; if longer than MaxExtraSubMenus, the excess is ignored.
      { get { return Extra_SubTitles._Copy(); }
        set { int len = value._Length();
              if (len > 0)
              { int minim = Math.Min(len, MaxExtraSubMenus);
                string[] trial = new string[minim];  bool dud = false;   string ss;
                for (int i=0; i < minim; i++) { ss = value[i].Trim(); if (ss != "") trial[i] = ss;  else { dud = true; break; } }
                if (!dud) Extra_SubTitles = trial;
      }     } }

//-----------------------------------

/// <summary>
/// Kills the current Extra menu system, main and submenu items, and replaces it. The submenus will have the same length as the string[]
/// field Extra_SubTitles (from 1 to MaxExtraSubMenus).
/// </summary>
    public void BuildExtraMenuSystem(bool isFirstTime)
    {// *** DON'T be tempted to rewrite this with an argument for visible/invisible menus, as it won't work (see note after 'ShowAll()' in Board constructor).
      if (!isFirstTime) MainMenu.Remove(ExtraMenu);
      ExtraMenu = new MenuItem(Extra_Title);
      MainMenu.Append(ExtraMenu);
      Menu ExtraSubMenus = new Menu();
      ExtraMenu.Submenu = ExtraSubMenus;
      // Extra submenus:
      int subsidy = Math.Min(MaxExtraSubMenus, Extra_SubTitles.Length);
      for (int sub = 0; sub < subsidy; sub++)
      { string ss = "ExtraSub" + (sub+1).ToString();
        MenuItem mite = new MenuItem(Extra_SubTitles[sub]);  mite.Name = ss;
        mite.Activated += new EventHandler(OnExtraActionsActivated);
        ExtraSubMenus.Append(mite);
        Extra_SubMenu[sub] = mite;
      }
    }

//-----------------------------------

// ********* INSTANCE ENTITIES ************************   //instance

  // ============= INSTANCE FIELDS ===================================
  // A few of these are labelled 'PUBLIC'. They are required by special classes (at the time of writing, only class CairoGraphic) which have
  // the right to grab a board. As most of the code inside this unit, and all outside of it, cannot grab a board, the qualifier 'public' does
  // not make the field generally accessible.

    private int boardID; // Set by instance constructor, and unique within the life of its parent class Board.
   // Basic structures:
    protected Gtk.Label LblHeader = null;
    protected Gtk.EventBox eboxHeader = null;
    protected Gtk.EventBox eboxDa = null;
    protected Gtk.VBox vboxBody = null;
    protected Gtk.HBox hboxDrawer = null;
    protected DrawingArea Da = null;
    protected Gtk.ScrolledWindow Scroller = null;
    protected Gtk.TextView TVBelow = null;
    protected int OrigReqWidth, OrigReqHeight; // Dimensions of the board as originally requested in static Board(.)
    protected int OrigReqHeaderHeight, OrigReqDrawingAreaHeight; // as above.
    protected int OrigReqScrollerHeight; // requested in the constructor, not by the user.
    protected bool OrigIncludeDescription; // as above
    protected double ExpansionRatio = 4.0; // If Hd is height of drawing area and Ht is height of text view, then when the box is
    public List<Graph> GraphsExtant = new List<Graph>(); // Public for access to static methods; but safe to be public, because Board
                                                         // instance pointers can't be accessed outside this unit.
    protected int NextTaskID = 1;
    public string ImageFileName = ""; // Set by the first call to SaveImage in the CairoGraphic class, but can be set beforehand.
                                      // File | Save Image will save without asking for a file name, if this is nonnull.
                                      // accessed via static 'Get..' 'Set..' properties, so that it can be got in class CairoHelper.
    public bool SaveImagePlease  = false; // Set TRUE by a file|save menu click; the code there also forces a redraw.
                                          // The redraw ( = OnExpose event) immediately resets SaveImagePlease to false after doing so.

    // Keypress fields: Nothing in this unit detects keypresses. The calling code should invoke a KeySnooper for the whole application,
    // and this should add the keypress details to the stack below (indirectly, via static method AddKeyPress(.)).
    // Other than that, the only way code should impact on these is to read the keypress stack (static GetKeyPresses(.)) and to reset
    // the stack to empty in the process (static ClearKeyPresses(.)).
    private Duo[] KeyPresses = new Duo[MaxKeyPresses]; // Set to all (0,0) in the constructor; [0] is latest entry; field .X is the key value,
        // .Y is the time of the keypress. Length is MaxKeyPresses, which just fits the largest allowed single keypress
        //  complex (i.e. helper keys + main key).
        // The static method RegisterKeyPress(.) not only registers key presses but also evokes the required actions and then deletes
        //  the key values from this stack.
    public Strint[] Keyings = new Strint[MaxKeyings]; // This stack has [0] as the latest entry, and is never cleared automatically;
        // the user is supposed to reference it and then clear the handled keying(s). Length is MaxKeyings. Field .I is a key value
        // (never a standard helper key), hopefully always a basic key on the keyboard; and field .S is one of exactly:
        //   "", "a", "c", "s", "ac", "as", "cs" and "acs", depending on the helper key combination (or lack thereof).
    /// <summary>
    /// If the submenu of a main graph menu has been clicked (not including the extra menu's submenus, if any), returns: .S = menu name,
    ///  which is the text on the submenu without its spaces; .X = millisecs since start of 1AD; .B TRUE; .I unused. After the initial
    ///  setting below, is never reset; so the user may choose e.g. to reset .B to 'false' after handling a click. As .I is never accessed
    ///  after the initialization below, .I holds whatever value the user may choose to put there.
    /// </summary>
    public Quad MainSubMenuClicked = new Quad(false);
    public string LblHeaderPresentedText = "", TVBelowPresentedText = ""; // A record of the texts, possibly marked up, originally
                // presented to the board; here for retrieval by Board.GetTexts(.).
    public Gtk.AccelGroup HotKeys; // Assigned in the constructor below.


 // ======= INSTANCE CONSTRUCTOR AND ON_DELETE METHOD =================================== //constructor
  /// <summary>
  /// BOARD is a passive window, created once and then destroyed once when all (re)graphing / (re)drawing is concluded.
  /// All drawing is done in the contained instance "Da" of a SEPARATE CLASS, class CairoGraphic. That instance has
  /// the same lifespan as the Board instance, but may well be subject to different graphs, drawings and images in its lifetime.
  /// </summary>
    protected Board (int ReqWidth, int ReqHeight, int ReqHeaderHeight, int ReqDrawingAreaHeight, bool IncludeDescription) : base(Gtk.WindowType.Toplevel)
    {// BUILD AND SHOW THE FORM
      for (int i=0; i < Extra_SubTitles.Length; i++) Extra_SubTitles[i] = i.ToString();
      this.Name = "JLib.Board";
      this.Title =  Mono.Unix.Catalog.GetString (" ");
      this.WindowPosition = (( Gtk.WindowPosition)(4));
      this.DestroyWithParent = true;
      // Container child JLib.Board.Gtk.Container+ContainerChild
      this.vboxBase = new  Gtk.VBox ();
      this.vboxBase.Name = "vboxBase";
      this.vboxBase.Spacing = 1;
    // SHORT CUT KEY collection for MainWindow instance:
      HotKeys = new AccelGroup(); // This will hold the hot keys for menus (or anything else, since it is owned by MainWindow).
      this.AddAccelGroup(HotKeys); // so far empty.
    // MAIN MENU -- this does not use Gtk.Action, whose fields are very inaccessible to the programmer.
      MainMenu = new MenuBar();   MainMenu.Name = "MainMenu";
      NoFixedMainMenuItems = 0; // **** If changing the menu system, also adjust static member SetFixedMenuVisibilities(.).
    // File menu:
      FileMenu = new MenuItem("_File");  FileMenu.Name = "File"; // '_' before a letter gives direct menu access from the keyboard: ALT + that letter.
      MainMenu.Append(FileMenu);
      NoFixedMainMenuItems++; // **** If changing the menu system, also adjust static member SetFixedMenuVisibilities(.).
      FileSubMenus = new Menu(); // This is a design shell; it leaves all the appearance and signalling stuff to its contained MenuItem list.
      FileMenu.Submenu = FileSubMenus; // A collection of all the submenus. (Pity the property name is note in the plural, as it is not just one submenu.)
        // You could concatenate the last two lines as "FileMenu.Submenu = new Menu();" but code would fail at 'Append' below - see the explanation there.
      // File | Save Image:
      File_SaveImage = new MenuItem("Save Image");  File_SaveImage.Name = "SaveImage";
      File_SaveImage.Activated += new EventHandler(OnSaveImageActionActivated);
      FileSubMenus.Append(File_SaveImage); // You can't shortcut by eliminating FileSubMenus and simply writing "FileMenu.Submenu.Append(.); this is
                                      // because "Submenu = " is just a property which adds to / reads from the collection FileSubMenus.
      // File | Save Image As:
      File_SaveImageAs = new MenuItem("Save Image As");  File_SaveImageAs.Name = "SaveImageAs";
      File_SaveImageAs.Activated += new EventHandler(OnSaveImageAsActionActivated);
      FileSubMenus.Append(File_SaveImageAs);
      // File | Exit:
      File_Exit = new MenuItem("E_xit");   File_Exit.Name = "Exit";
      File_Exit.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.x, Gdk.ModifierType.ControlMask, AccelFlags.Visible)); // Ctrl-X as hot key.
      File_Exit.Activated += new EventHandler(OnExitActionActivated);//FileQuit_Activated);
      FileSubMenus.Append(File_Exit);

    // Zoom menu:
      ZoomMenu = new MenuItem("_Zoom");  ZoomMenu.Name = "Zoom";
      MainMenu.Append(ZoomMenu);
      NoFixedMainMenuItems++; // **** If changing the menu system, also adjust static member SetFixedMenuVisibilities(.).
      ZoomSubMenus = new Menu();
      ZoomMenu.Submenu = ZoomSubMenus;
      // Zoom | In:
      Zoom_In = new MenuItem("Zoom In");  Zoom_In.Name = "ZoomIn";
      Zoom_In.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.i, Gdk.ModifierType.ControlMask, AccelFlags.Visible)); // Ctrl-I as hot key.
      Zoom_In.Activated += new EventHandler(OnZoomActionsActivated);
      ZoomSubMenus.Append(Zoom_In);
      // Zoom | Out:
      Zoom_Out = new MenuItem("Zoom Out");  Zoom_Out.Name = "ZoomOut";
      Zoom_Out.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.o, Gdk.ModifierType.ControlMask, AccelFlags.Visible)); // Ctrl-O as hot key.
      Zoom_Out.Activated += OnZoomActionsActivated;
      ZoomSubMenus.Append(Zoom_Out);
      // Zoom | In Horizontally:
      Zoom_In_H = new MenuItem("Zoom In Horizontally");  Zoom_In_H.Name = "ZoomInHorizontally";
      Zoom_In_H.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.i, Gdk.ModifierType. ControlMask | Gdk.ModifierType.Mod1Mask,
                                  AccelFlags.Visible)); // Alt-Ctrl-I as hot key.
      Zoom_In_H.Activated += OnZoomActionsActivated;
      ZoomSubMenus.Append(Zoom_In_H);
      // Zoom | Out Horizontally:
      Zoom_Out_H = new MenuItem("Zoom Out Horizontally");  Zoom_Out_H.Name = "ZoomOutHorizontally";
      Zoom_Out_H.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.o, Gdk.ModifierType. ControlMask | Gdk.ModifierType.Mod1Mask,
                                  AccelFlags.Visible)); // Alt-Ctrl-O as hot key.
      Zoom_Out_H.Activated += OnZoomActionsActivated;
      ZoomSubMenus.Append(Zoom_Out_H);
      // Zoom | In Vertically:
      Zoom_In_V = new MenuItem("Zoom In Vertically");  Zoom_In_V.Name = "ZoomInVertically";
      Zoom_In_V.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.i, Gdk.ModifierType. ControlMask | Gdk.ModifierType.ShiftMask,
                                  AccelFlags.Visible)); // Shift_Ctrl-I as hot key.
      Zoom_In_V.Activated += OnZoomActionsActivated;
      ZoomSubMenus.Append(Zoom_In_V);
      // Zoom | Out Vertically:
      Zoom_Out_V = new MenuItem("Zoom Out Vertically");  Zoom_Out_V.Name = "ZoomOutVertically";
      Zoom_Out_V.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.o, Gdk.ModifierType. ControlMask | Gdk.ModifierType.ShiftMask,
                                  AccelFlags.Visible)); // Shift_Ctrl-O as hot key.
      Zoom_Out_V.Activated += OnZoomActionsActivated;
      ZoomSubMenus.Append(Zoom_Out_V);



    // ViewPoint menu:
      ViewPointMenu = new MenuItem("_ViewPoint");
      MainMenu.Append(ViewPointMenu);
      NoFixedMainMenuItems++; // **** If changing the menu system, also adjust static member SetFixedMenuVisibilities(.).
      Menu ViewPointSubMenus = new Menu();
      ViewPointMenu.Submenu = ViewPointSubMenus;
      // ViewPoint | Small Left:  --- hot keys: small movements, cntrl-n;  large movements, shift-cntrl-n.
      ViewPoint_SmallLeft = new MenuItem("Move Left");  ViewPoint_SmallLeft.Name = "MoveLeft";
      ViewPoint_SmallLeft.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Left, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_SmallLeft.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_SmallLeft);
      // ViewPoint | Small Right:
      ViewPoint_SmallRight = new MenuItem("Move Right");  ViewPoint_SmallRight.Name = "MoveRight";
      ViewPoint_SmallRight.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Right, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_SmallRight.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_SmallRight);
      // ViewPoint | Small Up:
      ViewPoint_SmallUp = new MenuItem("Move Up");  ViewPoint_SmallUp.Name = "MoveUp";
      ViewPoint_SmallUp.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Up, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_SmallUp.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_SmallUp);
      // ViewPoint | Small Down:
      ViewPoint_SmallDown = new MenuItem("Move Down");  ViewPoint_SmallDown.Name = "MoveDown";
      ViewPoint_SmallDown.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Down, Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_SmallDown.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_SmallDown);
      // ViewPoint | Go Left:
      ViewPoint_BigLeft = new MenuItem("Jump Left");  ViewPoint_BigLeft.Name = "JumpLeft";
      ViewPoint_BigLeft.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Left,
                                                        Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_BigLeft.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_BigLeft);
      // ViewPoint | Go Right:
      ViewPoint_BigRight = new MenuItem("Jump Right");  ViewPoint_BigRight.Name = "JumpRight";
      ViewPoint_BigRight.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Right,
                                                        Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_BigRight.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_BigRight);
      // ViewPoint | Go Up:
      ViewPoint_BigUp = new MenuItem("Jump Up");  ViewPoint_BigUp.Name = "JumpUp";
      ViewPoint_BigUp.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Up,
                                                        Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_BigUp.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_BigUp);
      // ViewPoint | Go Down:
      ViewPoint_BigDown = new MenuItem("Jump Down");  ViewPoint_BigDown.Name = "JumpDown";
      ViewPoint_BigDown.AddAccelerator("activate", HotKeys, new AccelKey(Gdk.Key.Down,
                                                        Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask, AccelFlags.Visible));
      ViewPoint_BigDown.Activated += new EventHandler(OnViewPointActionsActivated);
      ViewPointSubMenus.Append(ViewPoint_BigDown);

    // Scaling menu:
      ScalingMenu = new MenuItem("_Scaling");
      MainMenu.Append(ScalingMenu);
      NoFixedMainMenuItems++; // **** If changing the menu system, also adjust static member SetFixedMenuVisibilities(.).
      ScalingSubMenus = new Menu();
      ScalingMenu.Submenu = ScalingSubMenus;
      // Scaling | New:
      Scaling_New = new MenuItem("New Scaling");  Scaling_New.Name = "NewScaling";
      Scaling_New.Activated += new EventHandler(OnNewScalingActivated);
      ScalingSubMenus.Append(Scaling_New);
      // Scaling | Original:
      Scaling_Original = new MenuItem("Original Scaling");  Scaling_Original.Name = "OriginalScaling";
      Scaling_Original.Activated += new EventHandler(OnOriginalScalingActivated);
      ScalingSubMenus.Append(Scaling_Original);
      // Aspect | New:
      Aspect_New = new MenuItem("New Aspect");  Aspect_New.Name = "NewAspect";
      Aspect_New.Activated += new EventHandler(OnNewAspectActivated);
      ScalingSubMenus.Append(Aspect_New);
      // Aspect | Original:
      Aspect_Original = new MenuItem("Original Aspect");  Aspect_Original.Name = "OriginalAspect";
      Aspect_Original.Activated += new EventHandler(OnOriginalAspectActivated);
      ScalingSubMenus.Append(Aspect_Original);

    // Extra menu:
      BuildExtraMenuSystem(true);
 
      this.vboxBase.Add (MainMenu);
       Gtk.Box.BoxChild w2 = (( Gtk.Box.BoxChild)(this.vboxBase[this.MainMenu]));
      w2.Position = 0;
      w2.Expand = false;
      w2.Fill = false;
      this.Add (this.vboxBase);
      if ((this.Child != null)) {
        this.Child.ShowAll ();
      }

      this.AllowShrink = true; // Without this, you can never make the board smaller than its startup value (though you can make it bigger).
      this.DefaultWidth = ReqWidth; // RHS is a static property that can be reset by the user.
      this.DefaultHeight = ReqHeight;
     // Main Window events
      this.DeleteEvent += new  Gtk.DeleteEventHandler (this.OnDeleteEvent);
      this.SizeRequested += new Gtk.SizeRequestedHandler(this.OnSizeRequested);

     // HEADER LABEL
      if (ReqHeaderHeight > 0)
      { eboxHeader = new Gtk.EventBox (); // Needed so we can set a background colour for the label.
        eboxHeader.Name = "eboxHeader";
        eboxHeader.ModifyBg(StateType.Normal, Graph.DefClrMarginBgd); // Last is public, so can be altered. But the label background,
                                // the graph's marginal space and the text view background must all be that same colour.
        OrigReqHeaderHeight = ReqHeaderHeight - 1; // We set vboxBase's .Spacing to 1 pixel, hence the ebox has 1 pixel of margin above & below.
        eboxHeader.HeightRequest = OrigReqHeaderHeight;
        LblHeader = new Gtk.Label ();
        LblHeader.Name = "LblHeader";
        LblHeader.UseMarkup = true;
        LblHeader.SingleLineMode = true;
        LblHeader.Layout.Alignment = Pango.Alignment.Center; // If you weren't using markup, LblHeader.Xalign would work; but never use .Justify.
        LblHeader.ModifyFg(StateType.Normal, JTV.Black); // The label's text colour.
          // The following is a fudge. Without it, the very first graph of the program may not have its heading centred, even though it will be
          //  centred after any resize, as then will subsequent graphs in subsequent runs:
        LblHeader.Text = "                                                                   ";
        eboxHeader.Add (LblHeader);
        vboxBase.Add (eboxHeader);
      }
    // FURTHER CONTAINERS
      vboxBody = new Gtk.VBox ();
      vboxBody.Name = "vboxBody";
      vboxBody.Spacing = 1;
      hboxDrawer = new Gtk.HBox ();
      hboxDrawer.Name = "hboxDrawer";
      hboxDrawer.Spacing = 1;
      vboxBody.Add (hboxDrawer);
      // The drawing area has to be placed inside an event box so that we can raise mouse click events:
      eboxDa = new Gtk.EventBox ();
      eboxDa.Name = "eboxDa";
      eboxDa.ButtonPressEvent += new ButtonPressEventHandler(OnMouseClick);
      eboxDa.ButtonReleaseEvent += new ButtonReleaseEventHandler(OnMouseUnclick);
      hboxDrawer.Add(eboxDa);
    // DRAWING AREA
      Da = new CairoGraphic(); // We will give it a name at the end of this constructor, when we have a BoardID.
      eboxDa.Add(Da);
      Da.HeightRequest = ReqDrawingAreaHeight;

     // DESCRIPTION TEXTVIEW
      if (IncludeDescription)
      { Scroller = new Gtk.ScrolledWindow ();
        Scroller.Name = "Scroller";
        Scroller.ShadowType = Gtk.ShadowType.None; // We don't want the description to have a visible border around it.
        OrigReqScrollerHeight = ReqHeight - ReqHeaderHeight - ReqDrawingAreaHeight - 29; // 29 = menu + 4 1-pixel borders.
        Scroller.HeightRequest = OrigReqScrollerHeight;
        TVBelow = new Gtk.TextView ();
        TVBelow.CanFocus = true;
        TVBelow.Name = "TVBelow";
        TVBelow.CursorVisible = false;
        TVBelow.WrapMode = ((Gtk.WrapMode)(2));
        TVBelow.LeftMargin = 25;  TVBelow.RightMargin = 25; // Set to be the same as the margin between graph perimeter and window edge.
                                                            // See Graph.SetParameters(.), local variable rightMargin.
        TVBelow.Justification = Justification.Fill; // If ".Left", text nearly always falls well short of the right margin.
        TVBelow.ModifyBase(StateType.Normal, Graph.DefClrMarginBgd); // Last is public, so can be altered. But the label background,
                                // the graph's marginal space and the text view background must all be that same colour.
        Scroller.Add (TVBelow);
        vboxBody.Add (Scroller);
      }
      vboxBase.Add (vboxBody);
     // SETTINGS
      boardID = NextBoardID;  NextBoardID++;
      BoardsRunning.Add(boardID, this); // register the board in the class's static dictionary of all running boards.
      Da.Name = "B" + boardID.ToString(); // The only way that Da's .OnExpose event can tell us where it belongs.
      // Info for use with resizing:
      OrigReqWidth = ReqWidth; OrigReqHeight = ReqHeight; // Dimensions of the board as originally requested in static Board(.)
      OrigReqDrawingAreaHeight = ReqDrawingAreaHeight; // as above.
      OrigIncludeDescription = IncludeDescription; // as above
      KeyPresses = new Duo[MaxKeyPresses];

     // SHOW:
      ShowAll();
      for (int i=0; i < Board.MaxKeyPresses; i++) this.KeyPresses[i] = new Duo(0,0);
      for (int i=0; i < Board.MaxKeyings; i++) this.Keyings[i] = new Strint(0,"");
      // These have to follow ShowAll(), or they have no effect - the menus remain visible.
      ExtraMenu.Visible = false;
      for (int i=0; i < MaxExtraSubMenus; i++) Extra_SubMenu[i].Visible = false;
    }

  /// <summary>
  /// Raised by clicks on the drawing area Da. Coordinates are relative to its drawing area, so that the same pixel reference system is used
  /// as is used to draw structures on Da.
  /// </summary>
  protected virtual void OnMouseClick(object o, ButtonPressEventArgs args)
  { Graph laugh = Board.GetFocussedGraph(this.boardID);  if (laugh == null) return;
    double pixX = args.Event.X,  pixY = args.Event.Y,  userX,  userY;
    Graph.ToUserCoords(laugh, pixX, pixY, out userX, out userY);
    MouseRec mickey = new MouseRec(this.boardID, laugh.ArtID, args.Event.Button, args.Event.Time, JS.Tempus('L', false), userX, userY, pixX, pixY);
    while (MouseClicks.Count >= MaxMouseClicks) MouseClicks.RemoveAt(MaxMouseClicks-1); // end up with one less than the maximum allowed.
    MouseClicks.Insert(0, mickey);
    ClickedGraph = laugh.ArtID;
  }

  /// <summary>
  /// Raised by releasing the mouse button clicked on the drawing area Da. Coordinates are relative to its drawing area, so that the
  /// same pixel reference system is used as is used to draw structures on Da. Note that unlike the OnMouseClick event, the result
  /// does not go onto a stack but to a single static field (accessed as Board.LastUnclick.)
  /// </summary>
  protected virtual void OnMouseUnclick(object o, ButtonReleaseEventArgs args)
  { Graph laugh = Board.GetFocussedGraph(this.boardID);  if (laugh == null) return;
    double pixX = args.Event.X,  pixY = args.Event.Y,  userX,  userY;
    Graph.ToUserCoords(laugh, pixX, pixY, out userX, out userY);
    LastMouseUnclick = new MouseRec(this.boardID, laugh.ArtID, args.Event.Button, args.Event.Time, JS.Tempus('L', false), userX, userY, pixX, pixY);
  }

  // This is here to avoid duplication of the Cairographic class's Draw method on resizing the board. Instead duplication
  //  occurs here (which presumably takes up less time).
  // *** It was remarked out in August 2014 to avoid Glib errors about negative integers; I only discovered in March 2015 that the cost of doing
  //     so was bad border-drag-resizing behaviour: the top label space expands uselessly, leaving the graph much smaller than you wanted.
  //     The bit "if (heightIncrease > )..." was added to avoid those negative integers, and now all works well.  (There are still other silly
  //     "Glib-CRITICAL" errors, but let's not worry about them.)
    protected virtual void OnSizeRequested (object o, Gtk.SizeRequestedArgs args)
    {// Reset the spacing of board components:
      eboxHeader.HeightRequest =  OrigReqHeaderHeight; // Label height does not change.
      double heightIncrease =  Allocation.Height -  OrigReqHeight; // increase in board height over the original (at board creation time).
      if (heightIncrease > 0) // it can be negative
      { if ( OrigIncludeDescription)
        { Da.HeightRequest = (int) ( OrigReqDrawingAreaHeight + ExpansionRatio * heightIncrease / (1.0 + ExpansionRatio) );
          Scroller.HeightRequest = (int) ( OrigReqScrollerHeight + heightIncrease / (1.0 + ExpansionRatio) );
        }
        else  Da.HeightRequest =  OrigReqDrawingAreaHeight + (int) heightIncrease;
      }
    }

    protected virtual void OnDeleteEvent (object o, Gtk.DeleteEventArgs args)
    {
      LastKilledBoard.X = boardID;
      Graph g = GetFocussedGraph(boardID);
      if (g != null) LastKilledBoard.Y = g.ArtID;  else LastKilledBoard.Y = 0;
      BoardsRunning.Remove(boardID); // Pull itself out of the dictionary. Also calls 'delete', which is a ref-count-reducer;
          // but if args.RetVal is set to true, the board remains, so there must be an internal reference still extant.
      args.RetVal = false; // Yes, you can destroy the window. ('true' forces window to stay open.)
    }



// ===================================================================
//     MAIN MENU                              //menu
// -------------------------------------------------------------------

// ---- FILE MENU:                            //file
    protected virtual void OnSaveImageActionActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      SaveImagePlease = true; // The redraw call (OnExpose event in class CairoGraphic) will do the saving, and then reset this to FALSE.
      if (ImageFileName == "") ImageFileName = FileNameDialog();
      if (ImageFileName != "")
      { SaveImagePlease = true; // The redraw call (OnExpose event in class CairoGraphic) will do the saving, and then reset this to FALSE.
        Board.ForceRedraw(this.boardID, false);
      }
    }
    protected virtual void OnSaveImageAsActionActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      ImageFileName = FileNameDialog();
      if (ImageFileName != "")
      { SaveImagePlease = true; // The redraw call (OnExpose event in class CairoGraphic) will do the saving, and then reset this to FALSE.
        Board.ForceRedraw(this.boardID, false);
      }
    }
    // **** I have not been able to raise the OnDeleteEvent from here, as it uses System.DeleteEventArgs, which
    //       is opaque in Mono. (You can do "Gtk.DeleteEventArgs dargs = new DeleteEventArgs();  OnDeleteEvent(0, dargs);",
    //       and indeed end up in the OnDeleteEvent handler; but args.RetVal there does nothing, as 'dargs' has null fields,
    //       so no closing happens.)
    protected virtual void OnExitActionActivated (object o, System.EventArgs args)
    { LastKilledBoard.X = boardID;
      Graph g = GetFocussedGraph(boardID);
      if (g != null) LastKilledBoard.Y = g.ArtID;  else LastKilledBoard.Y = 0;
      BoardsRunning.Remove(boardID);
      this.Destroy();
    }
    protected string FileNameDialog()
    { Gtk.FileChooserDialog fc = new Gtk.FileChooserDialog("Choose or supply file name for saving", this,
           FileChooserAction.Save, "Cancel",ResponseType.Cancel, "Save",ResponseType.Accept);
      fc.SetCurrentFolder(ImagePathName); // At startup is "/", bringing one to the root directory.
      string fileName = "";
      int outcome = fc.Run();
      if (outcome == (int) ResponseType.Accept) fileName = fc.Filename.Trim();
      fc.Destroy(); // Without this, the FileChooserDialog window won't get closed.
      if (outcome != (int) ResponseType.Accept || fileName == "") return "";
      ImagePathName = (Filing.Parse(fileName))[0];
      int p = fileName.LastIndexOf('.'), q = fileName.Length;
      if (p != -1 && p == q-1) fileName = fileName.Substring(0, q-1); // file name ended in '.', so remove it and don't add an extension.
      else if (p == -1) fileName += ".png"; // add the extension:
      // Extract the path name and record it:
      return fileName;
    }


// -------------------------------------------------------------------

// ---- ZOOM MENU:                            //zoom
    protected virtual void OnZoomActionsActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      int[] grr = Graphs(this.boardID);  if (grr == null) return;
      char c = '?';
      switch (MainSubMenuClicked.S)
      { case "ZoomIn":  c = 'B'; break; // 'B' for 'both (horiz and vert)'; upper case for 'in' (things get bigger), lower case for 'out'.
        case "ZoomOut": c = 'b'; break;
        case "ZoomInHorizontally":  c = 'H'; break;
        case "ZoomOutHorizontally": c = 'h'; break;
        case "ZoomInVertically":  c = 'V'; break;
        case "ZoomOutVertically": c = 'v'; break;
        default: break;
      }
      Zoom(grr[0], c);
    }
    // Service routine for the above, which can also be used by other calling code:
    protected void Zoom(int GraphID, char how)
    { double factor = Graph.ZoomFactor; // currently 2, but who knows what it will become?
      double H_Magnifier = 1.0, V_Magnifier = 1.0;
      switch (how)
      { case 'B': H_Magnifier = V_Magnifier = factor;  break;
        case 'b': H_Magnifier = V_Magnifier = 1/factor;  break;
        case 'H': H_Magnifier = factor;  break;
        case 'h': H_Magnifier = 1/factor;  break;
        case 'V': V_Magnifier = factor;  break;
        case 'v': V_Magnifier = 1/factor;  break;
        default: break;
      }
      Graph.Magnify(this.boardID, GraphID, H_Magnifier, V_Magnifier);
    }


// -------------------------------------------------------------------

// ---- VIEWPOINT MENU:                            //viewpoint
    protected virtual void OnViewPointActionsActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      int[] grr = Graphs(this.boardID);  if (grr == null) return;
      ChangeViewPoint(grr[0], ((MenuItem) o).Name);
    }
   // Service routine for the above, which can also be used by other calling code:
   protected void ChangeViewPoint(int GraphID, string how)
   { double upFactor = 0.0, rightFactor = 0.0; // multipliers of graph axis span
     if      (how == "JumpLeft")     rightFactor = -Graph.MoveFactor;
     else if (how == "JumpRight")    rightFactor =  Graph.MoveFactor;
     else if (how == "JumpUp")       upFactor    =  Graph.MoveFactor;
     else if (how == "JumpDown")     upFactor    = -Graph.MoveFactor;
     else if (how == "MoveLeft")  rightFactor = -Graph.SmallMoveFactor;
     else if (how == "MoveRight") rightFactor =  Graph.SmallMoveFactor;
     else if (how == "MoveUp")    upFactor    =  Graph.SmallMoveFactor;
     else if (how == "MoveDown")  upFactor    = -Graph.SmallMoveFactor;
     Graph.MoveViewPoint(this.boardID, GraphID, rightFactor, upFactor);
   }


// -------------------------------------------------------------------

// ---- SCALING MENU:                            //scaling
    protected virtual void OnNewScalingActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      if (this.GraphsExtant.Count == 0) return;
      Graph G = this.GraphsExtant[0];
      bool its3D = G.Is3D;
      string header = "ADJUST SCALING", preamble = "";
      string[] prompts, boxTexts;
      while (true)
      {
        if (its3D)
        { prompts = new string[] { "X Low:", "X High:", "no. X segments:", "Y Low:", "Y High:", "no. Y segments:",
                                             "Z Low:", "Z High:", "no. Z segments:"};
          boxTexts = new string[] { G.LowX.ToString(), G.HighX.ToString(), G.SegsX.ToString(),
                                           G.LowY.ToString(), G.HighY.ToString(), G.SegsY.ToString(),
                                           G.LowZ.ToString(), G.HighZ.ToString(), G.SegsZ.ToString() };
        }
        else
        { prompts = new string[] { "X Low:", "X High:", "no. X segments:", "Y Low:", "Y High:", "no. Y segments:"};
          boxTexts = new string[] { G.LowX.ToString(), G.HighX.ToString(), G.SegsX.ToString(),
                                           G.LowY.ToString(), G.HighY.ToString(), G.SegsY.ToString() };
        }
        int btn = JD.InputBox(header, preamble, prompts, ref boxTexts, "ACCEPT", "CANCEL");
        if (btn != 1) return;
        double trialLowX=0, trialHighX=0, trialLowY=0, trialHighY=0, trialLowZ=0, trialHighZ=0;
        int trialSegsX=0, trialSegsY=0, trialSegsZ=0;
        Quad outcome;
        double[] trial = boxTexts._ParseDubArray(out outcome);
        if (outcome.B)
        { trialLowX = trial[0];  trialHighX = trial[1];  trialSegsX = Convert.ToInt32(trial[2]);
          trialLowY = trial[3];  trialHighY = trial[4];  trialSegsY = Convert.ToInt32(trial[5]);
          if (its3D) { trialLowZ = trial[6];  trialHighZ = trial[7];  trialSegsZ = Convert.ToInt32(trial[8]); }
          if (trialLowX == trialHighX) preamble = "X Low and X High cannot be the same value";
          else if (trialLowY == trialHighY) preamble = "Y Low and Y High cannot be the same value";
          else if (its3D && trialLowZ == trialHighZ) preamble = "Z Low and Z High cannot be the same value";
          else if (trialSegsX < 1) preamble = "The no. of X segments must be an integer greater than zero";
          else if (trialSegsY < 1) preamble = "The no. of Y segments must be an integer greater than zero";
          else if (its3D && trialSegsZ < 1) preamble = "The no. of Z segments must be an integer greater than zero";
          else
          { G.LowX = trialLowX;  G.HighX = trialHighX;   G.SegsX = trialSegsX;
            G.LowY = trialLowY;  G.HighY = trialHighY;   G.SegsY = trialSegsY;
            if (G.Is3D) { G.LowZ = trialLowZ;  G.HighZ = trialHighZ;   G.SegsZ = trialSegsZ; }
            break; // The only way out of this loop, apart from the above 'return' for cancel.
          }
        }
        else preamble = "cannot translate '" + outcome.S + "' in the " + (outcome.I + 1)._Ordinal() + " input box";
      }
      // Can only get here if parsing was successful, and graph parameters were therefore updated.
      G.ForceRedraw();
    }

    protected virtual void OnOriginalScalingActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      int[] grr = Graphs(this.boardID);  if (grr == null) return;
      Graph.OriginalScale(this.boardID, grr[0]);
    }
    protected virtual void OnNewAspectActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      if (this.GraphsExtant.Count == 0) return;
      Graph G = this.GraphsExtant[0];
      string header = "ADJUST VIEWING ASPECT";
      string preamble1 = "<b>Entry format:</b>\n   <i>Degrees:</i>  just the value - e.g. '<span foreground=\"#FF00FF\">45</span>'.\n" +
          "   <i>Radians:</i>  prefix with R - e.g. '<span foreground=\"#FF00FF\">R 1.234</span>'.\n" +
          "   <i>Radians in terms of PI:</i>  also follow with 'PI':  '<span foreground=\"#FF00FF\">R 1 PI</span>'.";
      string[] prompts, boxTexts;
      while (true)
      { double ascensionDeg = G.Ascension * 180.0 / Math.PI;
        double ascensionPi = G.Ascension / Math.PI;
        double declinationDeg = G.Declination * 180.0 / Math.PI;
        double declinationPi = G.Declination / Math.PI;
        string preamble2 = "<b>Present ascension:</b>  <span foreground=\"blue\">" + ascensionDeg.ToString("F0") + " deg. (" +
                                G.Ascension.ToString("F3") + " rad., = " + ascensionPi.ToString("F3") + " PI.)</span>\n" +
                           "<b>Present declination:</b>  <span foreground=\"blue\">" + declinationDeg.ToString("F0") + " deg. (" +
                                G.Declination.ToString("F3") + " rad., = " + declinationPi.ToString("F3") + " PI.)</span>\n\n";
        prompts = new string[] { "Ascension:\n(0˚: X axis points to you; 90˚: to your right)",
                                 "Declination:\n(0˚: Z axis horizontal; 90˚: Z axis erect)     " };
        boxTexts = new string[2];
        int btn = JD.InputBox(header, preamble2 + preamble1, prompts, ref boxTexts, "ACCEPT", "CANCEL");
        if (btn != 1) return;
        bool successful = false, bothEmpty = true;
        double newAsc = G.Ascension, newDec = G.Declination;
        for (int i=0; i < 2; i++)
        { string ss = boxTexts[i].ToUpper()._Purge(' ', '\'', '\"'); // Remove spaces, and even quote marks (in case preamble instructions taken literally).
          if (ss == "") continue; // original value will be retained.
          bothEmpty = false;
          double trial = ss._ParseDouble(out successful);
          if (successful) trial = trial * Math.PI / 180.0; // was degrees, so convert to radians.
          else if (ss[0] == 'R')
          { trial = ss._Extent(1)._ParseDouble(out successful);
            if (!successful)
            { if (ss._Last(2) == "PI")
              { trial = ss._FromTo(1, ss.Length - 3)._ParseDouble(out successful);
                if (successful) trial *= Math.PI;
              }
            }
          }
          if (!successful) { JD.Msg("Format incorrect. Try again.");  break; } // breaks only from the 'for' loop.
          else { if(i == 0) newAsc = trial;  else newDec = trial; }
        }
        if (bothEmpty) return; // retain original values.
        if (successful)
        { G.Ascension = newAsc;   G.Declination = newDec;
        // Can only get here if parsing was successful, and graph parameters were therefore updated.
          G.ForceRedraw();
          break;
        }
      }
    }

    protected virtual void OnOriginalAspectActivated (object o, System.EventArgs args)
    { MainSubMenuClicked.S = ((Gtk.MenuItem) o).Name;
      MainSubMenuClicked.X = JS.Tempus('L', false);
      MainSubMenuClicked.B = true; // NB: .I is not accessed. The user is free to put stuff there; it will be untouched by this unit.
      if (this.GraphsExtant.Count == 0) return;
      Graph G = this.GraphsExtant[0];
      G.Ascension = G.OrigAscension;    G.Declination = G.OrigDeclination;
    }
// -------------------------------------------------------------------

// ---- EXTRA MENU:                           //extra
    /// <summary>
    /// Does nothing except set a value which calling code must detect by polling; it is up to calling code then to reset
    /// Board.ExtraSubMenuClicked to (0, -1) as the no-board, no-menu default. Note that submenus are numbered to base 1.
    /// </summary>
    protected virtual void OnExtraActionsActivated (object o, System.EventArgs args)
    { string name = ( (MenuItem) o).Name; // e.g. the first is "ExtraSub1" - the index of the menu item is found in name[8] onwards.
      int n = name._Extent(8)._ParseInt(-1);
      if (n >= 0 && n < MaxExtraSubMenus) ExtraSubMenuClicked = new Duo(this.boardID, n);
    }

// -------------------------------------------------------------------
// INSTANCE METHODS THAT ARE NOT EVENTS               //instance
// -------------------------------------------------------------------


//===============================================================
  } // END OF CLASS Board


//==================================================================================
// >>>>>>>>>>>>>>>>>>>>>>> THE CAIRO-DRIVEN DRAWING CLASS <<<<<<<<<<<<<<<<<<<<<<<<<<     //cairo   //draw
//==================================================================================
// The Board instance's "DrawingArea" widget named "Da" is an INSTANCE of this class, created at runtime and having the same
// lifespan as the Board instance. All the drawing information is contained in a CAIRO.CONTEXT object which is created in this
// class's OnExposeEvent (raised whenever (re)drawing is required - a new drawing, or adjustments to an existing one;
// or after resizing of the window holding the drawing). So the vital methods are:
// -- ON_EXPOSE_EVENT - which triggers drawing, and sets up the repository ('Context') from which drawings can be made;
// -- DRAW - which actually does the drawing, once handed that repository.

  public class CairoGraphic : DrawingArea
  {
/// <summary>
/// <para>Called when part or all of the board needs to be redrawn; i.e. (1) at startup and (2) after the board has been
/// resized. (It is not called when the window is uncovered after having been eclipsed by another widget). At this stage,
/// the drawing area and footer TextView still have their old size. Here we GET the new size of the drawing area (which
/// 'Big Brother' inside Gtk fixes), and can use this as ABSOLUTE sizing (no 'size request' here) for what we are drawing on it.</para>
/// </summary>
    protected override bool OnExposeEvent (Gdk.EventExpose args)               //expose  //redraw
    { Gdk.Window win = args.Window;
      Context g = Gdk.CairoHelper.Create (args.Window); // Context being the GRAPHICS DRAWING repository for the instance.
// *** NB! When switching from MonoDevelop 2.12 to 5.2.1 I noted the comment in the build log to the effect that g.Target is obsolete,
//  and that I should use 'Get/SetTarget()'. I did so. Result - a MonoMaths instance which works well enough when run from MonoDevelop,
//  but which crashes untraceably when MonoMaths is run outside of MonoDevelop. The line which seemed to crash it in particular was 
//  this:
//    var ThisTarget = g.GetTarget();
//  I could find absolutely nothing on the Internet after two days of extensive searching; there is simply no documentation.
//  So the best plan is to go on using the old format below until somebody manages to publish some help.

      int x, y, w, h, d; // 'd' - depth - = no. bits used in colour descriptions; always 24 for standard Gdk work.
      win.GetGeometry(out x, out y, out w, out h, out d); // For whole outer window, get top left cnr, width, height.
        // X comes out as 0, as if there were no margin; but H allows for a window margin of a few pixels either side.
        // Y starts below the title, at the top of the menu, but allows for a margin below the label. H: margin top and bottom.
      int boardID = (this.Name.Substring(1))._ParseInt(0);
      Board bordeaux = Board.GetBoardForExpose(this, boardID);  if (bordeaux == null) return false;
      foreach( Graph graph in bordeaux.GraphsExtant) graph.DrawGraph(g, w, h); // Plots will be drawn as well.
      if (bordeaux.SaveImagePlease)
      { string fnm = bordeaux.ImageFileName;
        if (fnm != "") { g.Target.WriteToPng(fnm);  bordeaux.SaveImagePlease = false; }
      }
      bordeaux = null; // The workings of this wrapper class are mysterious and nonstandard, so better dereference the pointer here.
      ((IDisposable) g.Target).Dispose (); // Everyone does this in Internet examples, so it seems to be essential.
      ((IDisposable) g).Dispose ();
      return true;
    }

  } // END OF CLASS CairoGraphic

//==============================================================================
//          THE DRAWING CLASSES
//==============================================================================
/// <summary>
/// <para>All Art objects use coordinates which are relative to the drawing surface (type double, in the range 0.0 to 1.0 inclusive)
/// and which increase from left to right and from BOTTOM TO TOP (i.e. in the opposite direction to screen coordinates).</para>
/// <para>Note that a Graph objects is bound on a 1:1 basis to boards (though a copy can be created for a new board). However
/// Plot objects </para>
/// </summary>
  public class Art                                                             //art
  { private static int nextArtID = 1; // NB: The actual art work will add either Board.GraphIDBase or Board.PlotIDBase to this.
    // The only INSTANCE field:
    protected int artID;
    public int ArtID { get { return artID; } }
    protected static long SourceValue; // Used to restrict access to certain calls between derived classes.

   // BASE CONSTRUCTOR.  NB: It is up to individual constructors to add the object to the ArtExtant dictionary.
    protected Art()
    { artID = nextArtID;  nextArtID++; // Remember, GraphIDBase or PlotIDBase must be added to this by child class constructors.
    }

  // COLOUR CONVERTERS
    public static Cairo.Color CairoColour(Gdk.Color Clr)
    { double multr = 1 / (double) 0x10000;
      return new Cairo.Color(multr * Clr.Red, multr * Clr.Green, multr * Clr.Blue); // The Gdk.Color fields are type uint.
    }
    public static Cairo.Color[] CairoColour(Gdk.Color[] Clr)
    { if (Clr == null) return null;
      int len = Clr.Length;
      Cairo.Color[] result = new Cairo.Color[len];
      double r,g,b, multr = 1 / (double) 0x10000;
      for (int i=0; i < len; i++)
      { r = multr * Clr[i].Red;  g = multr * Clr[i].Green;  b = multr * Clr[i].Blue;
        result[i] = new Cairo.Color(r, g, b);
      }
      return result;
    }
    public static Gdk.Color GdkColour(Cairo.Color Ccr)
    { double multr = (double) 255;
      return new Gdk.Color( Convert.ToByte(Ccr.R * multr), Convert.ToByte(Ccr.G * multr), Convert.ToByte(Ccr.B * multr) );
    }

    public static Gdk.Color[] GdkColour(Cairo.Color[] Ccr)
    { if (Ccr == null) return null;
      int len = Ccr.Length;
      Gdk.Color[] result = new Gdk.Color[len];
      double multr = (double) 255;
      for (int i=0; i < len; i++)
      { result[i] = new Gdk.Color( Convert.ToByte(Ccr[i].R * multr), Convert.ToByte(Ccr[i].G * multr), Convert.ToByte(Ccr[i].B * multr) ); }
      return result;
    }

  } // End of class Art
//================================================
  /// <summary>
  /// Base layer which might be relatively stable while different drawings are superimposed at different times.
  /// </summary>
  public class Graph : Art                   //graph
  {
  // STATIC FIELDS
    public static Gdk.Color  DefClrGraphBgd    = new Gdk.Color(0xE2, 0xEA, 0xFE); // Background of graph. Very pale blue.
    public static Gdk.Color  DefClrMarginBgd   = new Gdk.Color(0xF4, 0xF4, 0xF4); // Background of space around graph. Off-white.
    public static Gdk.Color  DefClrPerimeter = JTV.Blue; // Perimeter of the graphing surface (including the axes).
    public static Gdk.Color  DefClrHairLines   = JTV.Blue; // Hairlines across the graphing surface.
    public static Gdk.Color  DefClrTickStrings   = JTV.Blue;
    public static Gdk.Color  DefClrAxisName    = JTV.Blue;
    public static string DefFontName = "Ubuntu";
    public static int DefNameFontSize = 14, DefUnitsFontSize = 10; // Pixel sizes of axis names and of axis units (usually looks better smaller).
                                                            // Pixel size has to be set a bit higher than the desired point size.
    public static double ZoomFactor = 2,  MoveFactor = 0.5,  SmallMoveFactor = 0.1; // multipliers of graph axis span for menu actions
    public static double BasicMoveAngle = Math.PI / 4; // For 3D graph focus moves, [Small]MoveFactor will be multiplied by this angle.
    public static string DefValueFormat = "G4"; // arg for [double object].ToString(.). Not analysed.
    public static int StubLength = 5; // where an axis hairline is to be replaced by a stub a few pixels long, this is its length.
  // INSTANCE FIELDS
  // NB! Nearly all are public fields, and all affect the display. But the redrawing code does not bother to recalculate parameters every
  //  time it is redrawn, so it must be informed if it is to recalculate. To do this, call the graph's ForceRedraw function, which
  //  (a) sets  to the current time (and therefore ahead of LastGraphRecalcn); and (b) calls Board.ForceRedraw(.) on this board.
  // Properties are below associated fields, and are inset from them.
    protected int owningBoard = 0;
      public int OwningBoard { get { return owningBoard; } }
    public long GraphRevisedAt = 1; // It has to start higher than the next field, to force the first calculation of parameters.
    public long LastGraphRecalcn = 0;
    public List<Plot> PlotsExtant = new List<Plot>();
      public List<int> PlotsExtantIDs // List rather than int[], for ease of searching and sorting
      { get { List<int> result = new List<int>(); foreach(Plot pl in PlotsExtant) result.Add(pl.ArtID);  return result; } }
    protected bool is3D = false;
      public bool Is3D { get { return is3D; } }
    protected double boxLeft, boxRight, boxTop, boxBottom, boxWidth, boxHeight; // The first four are all will have 0.5 added to them,
      public double BoxLeft   { get { return boxLeft; } }                       //  so that perimeter lines and hair lines won't be aliassed
      public double BoxRight  { get { return boxRight; } }                      //  (and therefore blurred). This means that LoX, HiX etc. all
      public double BoxTop    { get { return boxTop; } }                        //  correspond to lines down the middle of a pixel column.
      public double BoxBottom { get { return boxBottom; } }
      public double BoxWidth  { get { return boxWidth; } }
      public double BoxHeight { get { return boxHeight; } }
    public double LowX, HighX, LowY, HighY, LowZ, HighZ; // User coordinates at axis extremes
    public int SegsX, SegsY, SegsZ; // No. segments into which hairlines divide the respective axes. (No hairlines = 1 segment).
    public double OrigLowX, OrigHighX, OrigLowY, OrigHighY, OrigLowZ, OrigHighZ; // Note - public. If the user wants to mess with these, go ahead!
    public int OrigSegsX, OrigSegsY, OrigSegsZ;
    public string AxisNameX="", AxisNameY="", AxisNameZ=""; // Short description of axis units - "Distance (kms.)" Left public, as SetParameters(.)
    // does not access it.
    protected bool NOAxisNameX = false, NOAxisNameY = false, NOAxisNameZ = false; // if TRUE, not only is no axis name displayed but also
    // there is no space for it.
  // TICK-RELATED arrays: A 'tick' is a mark on an axis, coinciding with a hairline position or an axis post position (i.e. ends of this axis).
  //  There are always Segs+1 ticks, and the two axis ends are always the first and the last tick. NB - There is no arrangement within this
  //  unit for a system where first and last hairlines do not coincide with axis posts. If you want otherwise you have to make the graph have
  //  no intrinsic hairlines (i.e. set Segs = 1) and then plot vertical and horizontal lines yourself as your home-made hairlines.
    protected double[] GapsX, GapsY, GapsZ; // Proportional widths of segments. Suppose the axis goes from user-coords. 10 to 80, with ticks to be at
    // user-coord X = 10, 20, 40, 80. Then GapsX could be { 10, 20, 40} - i.e. each gap is (Xn - Xn-1) - or anything in the same proportion:
    // { 1, 2, 4 } or {0.25, 0.5, 1 } would all be equivalent. If the input array has length < Segs, it is brought up to length by rotation
    // through all terms. (If null or empty, set to have same effect as if it were of length 1, { 1 } - i.e. evenly placed hairlines.) If
    // length >= Segs, the excess is simply ignored.
    // Obviously all values of Gaps must be positive. Any value <= 0 will be replaced by 1, which will be visually disturbing but prevent a crash.
    public string[] TickStringsX, TickStringsY; // Use for 2D: User's overriding strings at ticks on axes: {"0", "0.5", "1", ..}
    // DEFAULT: If null or empty or length < Segs+1, the shortfall is made up by automatic string generation; if longer, excess simply ignored.
    // 3D: SPECIAL USAGE: see next field...
    public string[] TickStringsZ; // 3D graphs don't have tick strings along axes; instead there is a comment to the right of the graph, "From: ...
    //  To: ... Step: ... ". So for 3D, TickStringsX/Y/Z must have exactly these 3 values (from / to / step), or it will be ignored.
    protected string[] tickstringsX, tickstringsY; // what is actually displayed; a blend of TickStringsX/Y and internal generation of strings.
    protected bool NOTickStringsX = false, NOTickStringsY = false; // if TRUE, no tick strings displayed AND also there is no space for them.
    // If false, then the arrays below are simply ignored. In the case of 3D, set by the constructors to TRUE,
    // and should never be reset by the user. No protection against doing so, but the result would be ugly and may even crash the program.
    public int[] TickStringsXVisible, TickStringsYVisible; // Suppose hairlines are numbered from 0 (the bottom / left graph margin) to
    // SegsX / SegsY (the top / right graph margin). Then the first value in this array is the index of the first hairline to have its tick string
    // visible (0 --> graph margin tick string is visible). Subsequent values are JUMPS; for example, the array {1, 2, 3 } would
    // cause hairlines 1, 3 and 6 to have visible tick strings. All values must be > 0, (though the first can be 0), or else the whole array
    // is ignored. An array of length less than 2, or null, is also ignored; in both cases the default state applies - no tick strings hidden.
    // The last gap is perpetuated across the scale; e.g. for { 1, 3 }, hairlines 1, 4, 7, 10, ... would have visible tick strings.
    // Note that if TSV[0] is > no. of segments, no tick strings will be visible; e.g. it can be set to Int32.MaxValue to achieve this.
    public byte[] HairsXVisible, HairsYVisible, HairsZVisible; // 0 = not visible at all; 1 = visible as a short stub;
    // 2 = hairline across width/ht. of graph. Rotates through all elements. (Where a hairline overlies an axis post its value is irrelevant
    // as the axis post is always drawn; this will involve HairsVisible[0] in the first rotation and HairsVisible[last] in the last rotation.)
    // DEFAULT: null or empty --> length 1, { false }, meaning that no hairlines will be visible.
    // NB - for 3D, 'HairsXVisible' allows the drawing of two sets of hairlines perp. to the X axis, one in the XY plane and one in the XZ plane.
    public bool NOHairsXVisible = false,  NOHairsYVisible = false,  NOHairsZVisible = false; // Leaves the above arrays as is, but suppresses
    // showing of their hairlines.
    public double Declination, Ascension; // Declination is the angle between the Z axis (positive arm) and the horizontal plane of the viewer,
    // and is 0 when the axis is pointing at the viewer and PI/2 when it is pointing heavenward. (The Z axis may only ever lie within the vertical
    // plane of the viewer.)
    // Ascension: If, for any deployment of 3D axes, the only change were to rotate the Z axis to have declination of PI/2, then this is the angle
    // made by the X axis (which is then in the horizontal plane). If its positive arm is pointing at the viewer, then its ascension is 0.
    // It rises through positive values if the axis rotates to the right (i.e. anticlockwise as viewed by a fly hovering above the
    // positive end of the Z axis). The Y axis always has an ascension PI/2 advanced from the X axis.
    // Default values of Declination and Ascension are set in the 3D constructors.
    public double OrigDeclination, OrigAscension;
    protected double pivotX, pivotY, rayLen; // Used in 3D only.
      public double PivotX { get { return pivotX; } }
      public double PivotY { get { return pivotY; } }
      public double RayLen { get { return rayLen; } }

   // RARELY ALTERED FIELDS
   // Colours (all use Gdk colours, which are converted internally to Cairo colours)
    public Gdk.Color  ClrGraphBgd = DefClrGraphBgd, ClrMarginBgd = DefClrMarginBgd, ClrPerimeter = DefClrPerimeter,
                      ClrHairLines = DefClrHairLines, ClrTickStrings = DefClrTickStrings, ClrAxisName = DefClrAxisName;
    public string FontName; // If not in the system, some machine default will be used. Can't use a list of families.
    public int NameFontSize, UnitsFontSize; // Pixel sizes of axis names and of axis units (usually looks better smaller).
                                                      // Pixel size has to be set a bit higher than the desired point size.
      // Fields to do with internal calculation of unit strings for axis:
    public string SuffixX,  SuffixY,  SuffixZ; // E.g. add " π" to values.
    public bool SuppressValueIfOneX,  SuppressValueIfOneY,  SuppressValueIfOneZ; // Esp. for use with suffixes - e.g. no '1' in: "1 π, 2 π, 3 π, ..."
    public bool SuppressSuffixIfZeroX,  SuppressSuffixIfZeroY,  SuppressSuffixIfZeroZ; // e.g. no '0' in "0 π, 2 π, 4 π, ..."
    public double FudgeFactorX,  FudgeFactorY,  FudgeFactorZ; // Multiplier, e.g. to convert between units without changing hairline posns.
    public string ValueFormatX,  ValueFormatY,  ValueFormatZ; // arg for [double object].ToString(.). Not analysed.

    // INTERNALLY SET FIELDS
    protected double lastWidth, lastHeight; // set in SetParameters(); enables check for resizing of board.
//##### Yet to adapt for 3D:
    protected double topMargin, rightMargin, heightNameX, heightUnitsX, widthNameY, leftNameY, widthUnitsY, leftUnitsY;
    protected double viewLeft=0, viewTop=0, viewWidth=0, viewHeight=0; // Used only for 3D; the viewing area for the 3D graph, within the graph 'box'.
    protected double[] ticksX, ticksY, ticksZ; // pixel coords. (within drawing area) of hairline-axis intercepts, including the corners
    public Duplex UserO, UserX, UserY, UserZ; // only used in the case of 3D, being axis terminal points. (In 2D, axes coincide with box margins.)
    // They code the pixel coords. of origin and the tips of the X,Y and Z "user" axes (as opposed to "native" axes).
    protected Tint  TintGraphBgd, TintMarginBgd, TintPerimeter, TintHairLines, TintTickStrings, TintAxisName;


// - - - - - - - - - - - - - - - -
// CONSTRUCTORS
// - - - - - - - - - - - - - - - -
    private Graph() : base()
    { artID += Board.GraphIDBase;
      // Convert colours from type Gdk.Color to type Cairo.Color:
      TintGraphBgd  = new Tint(ClrGraphBgd);
      TintMarginBgd = new Tint(ClrMarginBgd);
      TintPerimeter = new Tint(ClrPerimeter);
      TintHairLines = new Tint(ClrHairLines);
      TintTickStrings = new Tint(ClrTickStrings);
      TintAxisName  = new Tint(ClrAxisName);


      FontName = DefFontName;
      NameFontSize = DefNameFontSize;   UnitsFontSize = DefUnitsFontSize;
      GapsX = GapsY = GapsZ = new double[] {1};
      TickStringsX = TickStringsY = TickStringsZ = null;
      tickstringsX = tickstringsY = null; // no 'Z' equivalent.
      TickStringsXVisible = TickStringsYVisible = null; // in effect, every hairline will be visible.
      HairsXVisible = HairsYVisible = HairsZVisible = new byte[] {2};
      SuffixX = SuffixY = SuffixZ = "";
      SuppressValueIfOneX   = SuppressValueIfOneY   = SuppressValueIfOneZ   = false;
      SuppressSuffixIfZeroX = SuppressSuffixIfZeroY = SuppressSuffixIfZeroZ = false;
      FudgeFactorX = FudgeFactorY = FudgeFactorZ = 1.0;
      ValueFormatX = ValueFormatY = ValueFormatZ = DefValueFormat;
    }
    // Intended for 2D graphs.
    // Errors result from: Board unrecognized; Xlow = Xhigh (but it may be > Xhigh); Xsegs < 1.
    // If there is error, the returned object has its ArtID set to 0, incomplete fields, and is not implanted in a board.
    public Graph(int BoardID, double Xlow, double Xhigh, int Xsegs, double Ylow, double Yhigh, int Ysegs) : this ()
    { if (!Board.Exists(BoardID))
      { JD.Msg("Graph creation failed: board no. " + BoardID.ToString() + " could not be identified"); artID = 0;  return; }
      owningBoard = BoardID;
      if (Xhigh == Xlow || Yhigh == Ylow) { JD.Msg("Graph creation failed: either HighX = LowX or HighY = LowY"); artID = 0;  return; }
      if (Xsegs < 1 || Ysegs < 1)  { JD.Msg("Graph creation failed: either SegsX or SegsY is not >= 1"); artID = 0;  return; }
      LowX = Xlow; HighX = Xhigh; LowY = Ylow; HighY = Yhigh;  SegsX = Xsegs; SegsY = Ysegs;
      OrigLowX = LowX;  OrigHighX = HighX;  OrigLowY = LowY;  OrigHighY = HighY;
      OrigSegsX = SegsX;  OrigSegsY = SegsY;
      // These are initial values of the booleans. The user may reset any of them for the same drawing later on.
      int n = Board.AddGraph(BoardID, this);
      // *** Remove the following after a reasonable period; I can't foresee any situation where it would arise...
      if (n != 1) { JD.Msg("Graph creation failed: code " + n.ToString() + "; it's probably the programmer's fault"); artID = 0;  return; }
      Board.SetMenusFor2Dv3D(BoardID, false);
    }
    // Intended for 3D graphs.
    // Errors result from: Board unrecognized; Xlow = Xhigh (but it may be > Xhigh); Xsegs < 1.
    // If there is error, the returned object has its ArtID set to 0, incomplete fields, and is not implanted in a board.
    public Graph(int BoardID, double Xlow, double Xhigh, int Xsegs, double Ylow, double Yhigh, int Ysegs, double Zlow, double Zhigh, int Zsegs)
                   : this (BoardID, Xlow, Xhigh, Xsegs, Ylow, Yhigh, Ysegs)
    { if (artID == 0) return; // an error message was displayed by the called constructor.
      is3D = true;
      if (Zhigh == Zlow) { JD.Msg("Graph creation failed: HighZ = LowZ"); artID = 0;  return; }
      if (Zsegs < 1)     { JD.Msg("Graph creation failed: SegsZ is not >= 1"); artID = 0;  return; }
      OrigLowZ = LowZ = Zlow;   OrigHighZ = HighZ = Zhigh;   OrigSegsZ = SegsZ = Zsegs;
      // Note that the called constructor has already registered this graph in the board.
      // The following should be defined as units of (BasicMoveAngle / 2), this being the adjustment made by menu / hotkey for small mvmts.
      //  That way it will be possible to rotate to ascensions and declinations which are exact multiples of a right angle.
      OrigDeclination = 1.5 * BasicMoveAngle;  OrigAscension = - 0.5 * BasicMoveAngle; // A suitable neutral starting position for the run-of-the-mill 3D graph.
      Declination = OrigDeclination;  Ascension = OrigAscension;
      NOTickStringsX = true;  NOTickStringsY = true;
      // Axis terminal points (pixels) are needed for 3D only (as in 2D axes coincide with the box margins):
      UserO = new Duplex();  UserX = UserO;  UserY = UserO;  UserZ = UserO;
      Board.SetMenusFor2Dv3D(BoardID, true);
    }


// - - - - - - - - - - - - - - - -
// INSTANCE METHODS
// - - - - - - - - - - - - - - - -
    /// <summary>    ***** As of 10 April, 2012, this method is not called by any code in this unit or in MonoMaths. *****
    /// <para>The individual fields are all public and so can be set separately; but this method allows all 5 to be set at once. There is no way of
    ///  signalling that you want one or two of the fields left as is; if you are only changing, say, 4 of the fields, the argument for
    ///  the remaining field would have to be the existing field (e.g. "thisGraph.AxisNameX").</para>
    /// <para>Note that the bools NOAxisNameX/Y and NOTickStringsX/Y are not accessed here; they must be set separately (the constructor
    ///  default being 'false' for all 4).</para>
    /// <para>'Axis': Must be one of 'X','x','Y','y'.</para>
    /// <para>ERRORS result in no action (but no exception raised). They arise if Axis is not as above, or if the graph's ID is 0.</para>
    /// </summary>
    public void FurnishAxis(char Axis, string AxisName, double[] Gaps, string[] TickStrings,  int[] TickStringsVisible, byte[] HairsVisible)
    { if (this.artID < 1) return; // No error message, but no action either.
      bool isX = (Axis == 'X' || Axis == 'x'),   isY = (Axis == 'Y' || Axis == 'y');
      if (!isX && !isY && Axis != 'Z' && Axis != 'z') return;  // No error message, but no action either.
      if (isX) AxisNameX = AxisName; else if (isY) AxisNameY = AxisName; else AxisNameZ = AxisName;
      int glen = Gaps._Length();   if (glen < 1)  Gaps = new double[] {1.0};
      if (isX) GapsX = Gaps._Copy();  else if (isY) GapsY = Gaps._Copy();  else GapsZ = Gaps._Copy();
      int slen = TickStrings._Length();  if (slen < 1)  TickStrings = null;
      if (isX) TickStringsX = TickStrings._Copy();  else if (isY) TickStringsY = TickStrings._Copy();  else TickStringsZ = TickStrings._Copy();
      int vlen = TickStringsVisible._Length();  if (vlen < 2)  TickStringsVisible = null;
      if (isX) TickStringsXVisible = TickStringsVisible._Copy();  else if (isY) TickStringsYVisible = TickStringsVisible._Copy();
      int hlen = HairsVisible._Length();   if (hlen < 1)  HairsVisible = new byte[] { 2};
      if (isX) HairsXVisible = HairsVisible._Copy();  else if (isY) HairsYVisible = HairsVisible._Copy();  else HairsZVisible = HairsVisible._Copy();
    }

    /// <summary>
    /// This should be called after any set of changes to the graph; if it is not called, the changes still exist
    /// in the graph instance's fields but will mostly not affect the graph display, as internal recalculations will not occur.
    /// Sets LastGraphRecalcn before forcing the redraw.
    /// </summary>
    public void ForceRedraw()
    { if (this.artID < 1) return; // No error message, but no action either.
      GraphRevisedAt = DateTime.Now.Ticks;
      Board.ForceRedraw(owningBoard, true);
    }

    /// <summary>
    /// This will be aborted if there has not been a revision of fields or if the main window has been resized since the last call.
    /// </summary>
    private void SetParameters(Cairo.Context gr, double width, double height)
    {
 // ###### I have turned the next line off because Gtk redraws haphazardly, and sometimes does not force a redraw between adjustments
 //  to the graph, so that SetParameters does not run. I have not noticed any performance cost in turning it off. It remains to remove
 // all traces of the timing system.
 //if (GraphRevisedAt < LastGraphRecalcn && lastWidth == width && lastHeight == height) return; // no param. adjustments, no window resizing.

      if (!NOTickStringsX)
      { ProcessTickStrings(ref TickStringsX, ref tickstringsX, TickStringsXVisible, LowX, HighX, SegsX, SuffixX, SuppressValueIfOneX,
                            SuppressSuffixIfZeroX, FudgeFactorX, ValueFormatX);
      }
      if (!NOTickStringsY)
      { ProcessTickStrings(ref TickStringsY, ref tickstringsY, TickStringsYVisible, LowY, HighY, SegsY, SuffixY, SuppressValueIfOneY,
                             SuppressSuffixIfZeroY, FudgeFactorY, ValueFormatY);
      }
//      if (!NOTickStringsZ)
//      { ProcessTickStrings(ref TickStringsZ, ref tickstringsZ, TickStringsZVisible, LowZ, HighZ, SegsZ, SuffixZ, SuppressValueIfOneZ,
//                             SuppressSuffixIfZeroZ, FudgeFactorZ, ValueFormatZ);
//      }
      rightMargin = 25.0; // set to be the same as the board's TVBelow default setting of right margin.
      // Other fields depend on text sizing:
      heightUnitsX = 0.0;  widthUnitsY = 0.0;
      heightNameX = 0.0;  widthNameY = 0.0;
      if (!NOTickStringsX || !NOTickStringsY || !NOAxisNameX || !NOAxisNameY) // if all false, then no point in making time-consuming font settings.
      { gr.SelectFontFace(FontName, FontSlant.Normal, FontWeight.Normal); // Same font for both axis Units and axis Name.
        if (!NOTickStringsX || !NOTickStringsY)
        { gr.SetFontSize(UnitsFontSize); // Arg. is in user sizing units. We have used pixels for sizing, so this is pixel size.
          if (!NOTickStringsX) heightUnitsX = gr.TextExtents("X").Height + 5.0; // The addend is nec.to stop letters hitting the axis.
          if (!NOTickStringsY)
          { double wuy;
            for (int i=0; i <= SegsY; i++)
            { wuy = gr.TextExtents(tickstringsY[i]).Width;  if (wuy > widthUnitsY) widthUnitsY = wuy; }
          }
        }
        if (!NOAxisNameX || !NOAxisNameY)
        { gr.SetFontSize(NameFontSize); // Arg. is in user sizing units. We have used pixels for sizing, so this is pixel size.
          double ltrHt = gr.TextExtents("X").Height;
          if (!NOAxisNameX) heightNameX = ltrHt + 5.0; // The addend is nec.to stop letters hitting the axis.
          if (!NOAxisNameY) widthNameY = ltrHt;
        }
      }
      topMargin = heightUnitsX; // This allows the topmost Y-axis unit string to extend this far above the graph's top line.
      leftUnitsY = 15.0;
      leftNameY = rightMargin;
      // Now set the box size, based on the above:
      boxLeft = Math.Round(leftNameY + widthNameY + leftUnitsY + widthUnitsY, MidpointRounding.AwayFromZero) + 0.5; // XLo is then at middle of pixel col.
      boxTop  = Math.Round(topMargin + 0.5, MidpointRounding.AwayFromZero) + 0.5;
      boxWidth = width - (boxLeft - 0.5) - rightMargin; // Remove the 0.5 added above to boxLeft to make boxWidth integral (for better aliassing).
      boxHeight = height - (boxTop - 0.5) - (heightNameX + heightUnitsX + 5.0); // Remove the 0.5 for the same reason.
      boxRight = boxLeft + boxWidth;
      boxBottom = boxTop + boxHeight;
      if (is3D) // then define the 'viewer' (in which is the 3D axis system and plots) inside the 'box' (which also includes printed info.).
                // Drawing of axes and plots will extend right up to the edges of the viewer, but the edges will not be drawn; they are virtual.
      { int mgn = 20; // margin between the box edge and the viewer edge, on three sides (top, left and bottom).
        viewLeft = boxLeft + mgn;
        viewTop = boxTop + mgn;
        viewHeight = boxHeight - 2 * mgn;
        viewWidth = viewHeight; // The user must ensure that box width is at least half as wide again; that is, that boxWidth is at least
            // 1.5 * boxHeight. If so, printing to the right of the viewer will be properly visible; otherwise, who knows what will happen?
            // I am not going to build in any error detection.
        // The NATIVE axes will be pinioned to the pivot, which is right in the centre of the viewer:
        pivotX = viewLeft + viewWidth / 2.0;
        pivotY = viewTop + viewHeight / 2.0;
        rayLen = viewWidth / 2; // These native axes will have their distal ends on a sphere around the pivot with this radius.
      }

    // Prepare arrays used for drawing hairlines and writing unit strings beside them
      if (is3D)
      {

      }
      else // 2D graph:
      { ticksX = DevelopTicks2D(boxLeft, boxWidth, SegsX, GapsX);
        ticksY = DevelopTicks2D(boxBottom, -boxHeight, SegsY, GapsY); // Negative boxHeight allows for pixel Y decreasing as user Y increases.
      }
      if (HairsXVisible._Length() < 1) HairsXVisible = new byte[] { 0 }; // show no hairlines.
      if (HairsYVisible._Length() < 1) HairsYVisible = new byte[] { 0 }; // show no hairlines.
      if (HairsZVisible._Length() < 1) HairsZVisible = new byte[] { 0 }; // show no hairlines.
     // Closing Tasks
      lastWidth = width;  lastHeight = height;
      LastGraphRecalcn = DateTime.Now.Ticks;
    }
    // Utility method for the above.
    protected double[] DevelopTicks2D(double LowEndPixelPosn, double AxisPixelExtent, int Segs, double[] Gaps)
    { List<double> ticklist = new List<double>(Segs+1);
      ticklist.Add(0.0); // The first tick will always be the left end of the axis..
      int len = Gaps._Length(); // progressively add the gaps to get the tick positions
      if (len <= 0) { Gaps = new double[] {1.0};  len = 1; } // 'Gaps' isn't a REF arg., so resetting the pointer thus does nothing to ext'l value.
      double x, sum = 0;
      int cnt = 0;
      while (ticklist.Count < Segs+1)
      { x = Gaps[cnt];  if (x <= 0.0) x = 1.0; // The punishment for a silly value is a silly graph.
        cnt++; if (cnt == len) cnt = 0; // ensures rotation through GapsX, if it is short.
        sum += x;
        ticklist.Add(sum);
      }
      double[] ticks = ticklist.ToArray();
      // convert to pixel-offsets from the drawing area's edge:
      double factor = AxisPixelExtent / sum;
      for (int i=0; i <= Segs; i++) { ticks[i] = LowEndPixelPosn + ticks[i] * factor; }
      return ticks;
    }

//-----------------------------------------------------
// METHODS INVOLVED IN PROJECTING FROM 3D SPACE TO THE 2D VIEWER
// Native coordinates have zero projected to the middle of the viewer; and the sphere with a projection just filling the viewer
//  has radius 1.

    /// <summary>
    /// Given the NATIVE coordinates of a 3D point, return its projection to the graph.
    /// </summary>
    private Duplex ProjectionNative(double XCoordNative, double YCoordNative, double ZCoordNative)
    {
      double XprojnX = pivotX + XCoordNative * Math.Sin(Ascension) * rayLen,
             XprojnY = pivotY + XCoordNative * Math.Cos(Ascension) * Math.Cos(Declination) * rayLen,
             YprojnX = pivotX + YCoordNative * Math.Cos(Ascension) * rayLen,
             YprojnY = pivotY - YCoordNative * Math.Sin(Ascension) * Math.Cos(Declination) * rayLen,
             ZprojnX = pivotX,
             ZprojnY = pivotY - ZCoordNative * Math.Sin(Declination) * rayLen;
      double PtX = XprojnX + YprojnX + ZprojnX - 2 * pivotX;
      double PtY = XprojnY + YprojnY + ZprojnY - 2 * pivotY;
      return new Duplex(PtX, PtY);
    }
    /// <summary>
    /// <para>Elements of Texts_Positions use fields thus: text is in .SX, and the top left of its first glyph is to be at (.X, .Y).
    /// (Field .SY is unused.) The graph's FontName field is used, but the caller  sets the font size.</para>
    /// <para>'ReturnGlyphDims': If TRUE, the return is the Duplex (Width of a capital 'O', height of same);
    ///  if FALSE, the return is simply (0,0).</para>
    /// <para>Erratic arguments --> nothing prints, and (0,0) is returned.</para>
    /// </summary>
    protected static Duplex TextOnGraph(Graph Gph, Cairo.Context gr, int FontSz, bool ReturnGlyphDims, params Strub2[] Texts_Positions)
    { Duplex result = new Duplex(0,0);
      int noTexts = Texts_Positions.Length;   if (noTexts == 0) return result;
      Pango.Layout layout = Pango.CairoHelper.CreateLayout(gr);
      layout.FontDescription = Pango.FontDescription.FromString(Gph.FontName + " " + FontSz.ToString());
      if (ReturnGlyphDims)
      { layout.SetText("O");  Pango.Rectangle whatwewant, dummy;   layout.GetExtents(out dummy, out whatwewant);
        // Extents are returned in Pango Scale Units, and so must be converted to points:
        result = new Duplex( JTV.ToPointsDouble(whatwewant.Width), JTV.ToPointsDouble(whatwewant.Height));
      }
      gr.NewPath();
      for (int i = 0; i < noTexts; i++)
      { Strub2 scrub = Texts_Positions[i];
        gr.MoveTo(scrub.X, scrub.Y);
        layout.SetText(scrub.SX);  Pango.CairoHelper.ShowLayout(gr, layout);
      }
      return result;
    }


// END OF METHODS for projecting from 3D space to 2D space
//-----------------------------------------------------

   /// <summary>
    /// Although this method is public, it would be a daunting task for external code to call it directly, as it would be hard to
    /// work up a Cairo.Context object for its first argument. It is intended to be called from one place only, the OnExpose event
    /// of the board's drawing area.
    /// </summary>
    public void DrawGraph(Cairo.Context gr, double width, double height)
    { if (this.artID < 1) return; // No error message, but no action either.
      double PIPI = 2 * Math.PI;
            Tint tintGrey = new Tint(JTV.Grey), tintBlack = new Tint(JTV.Black);
      SetParameters(gr, width, height); // Quickly aborted if there is no need to recalculate because no relevant changes have occurred.
     // Colour the background for the whole drawing area:
      gr.SetSourceRGB(TintMarginBgd.r,  TintMarginBgd.g,  TintMarginBgd.b);
      gr.PaintWithAlpha(1.0); // It doesn't work to incorporate this into a "SetSourceRGBA(.)" call with the above; try it if you don't believe me.
     // Draw and fill the rectangle of the graph's perimeter:
      gr.LineWidth = 1;
      gr.Rectangle (boxLeft, boxTop, boxWidth, boxHeight);
      gr.SetSourceRGB(TintGraphBgd.r,  TintGraphBgd.g,  TintGraphBgd.b);
      gr.FillPreserve();
      gr.SetSourceRGB(TintPerimeter.r, TintPerimeter.g, TintPerimeter.b);
      gr.Stroke ();
     // Draw the frame, if this is 3D:
      if (is3D) // Then the coordinate frame has to be drawn. (For 2D, the coordinate frame is coincident with the graph box perimeter.)
      { double coeff = 0.61; // Value ensures that at its worst, the rotating frame in the viewer will just touch the edges of the box.
      // Draw and fill the rectangle of the viewer's perimeter. All of the following Duplexes are in NATIVE coordinates.
      // Suppose the positive NATIVE axes are all pointing towards us. Then the USER ORIGIN is the corner furthest from us; the USER
      // POSITIVE AXES are the three edges radiating from that corner, the user X axis being on our left. In drawing the frame, we will
      // use this notation for the corners: 'User' + whichever of user coordinates X,Y and Z is/are not zero (the origin being 'UserO').
      // Hence the positive tips of the X, Y and Z axes are UserX, UserY and UserZ; and the point furthest from the origin is UserXYZ.
        UserO  = ProjectionNative(-coeff, -coeff, -coeff);
        // Positive ends of the user axes:
        UserX  = ProjectionNative( coeff, -coeff, -coeff);
        UserY  = ProjectionNative(-coeff,  coeff, -coeff);
        UserZ  = ProjectionNative(-coeff, -coeff,  coeff);
        Duplex UserXY = ProjectionNative( coeff,  coeff, -coeff);
        // We can more quickly derive the remaining 3 corners from these.
        double vert = UserO.Y - UserZ.Y; // vertical distance between projected top-face corners and projected bottom-face corners
        Duplex UserXZ  = new Duplex(UserX.X,  UserX.Y  - vert);
        Duplex UserYZ  = new Duplex(UserY.X,  UserY.Y  - vert);
        Duplex UserXYZ = new Duplex(UserXY.X, UserXY.Y - vert);
        // Draw the user axes as thicker, with line width 2:
        gr.LineWidth = 2;
        gr.SetSourceRGB(TintPerimeter.r, TintPerimeter.g, TintPerimeter.b);
        gr.MoveTo(UserO.X, UserO.Y);  gr.LineTo(UserX.X, UserX.Y); // X axis
        gr.MoveTo(UserO.X, UserO.Y);  gr.LineTo(UserY.X, UserY.Y); // Y axis
        gr.MoveTo(UserO.X, UserO.Y);  gr.LineTo(UserZ.X, UserZ.Y); // Z axis
        gr.Stroke();
        // Draw the rest of the frame, using line width 1:
        gr.LineWidth = 1;
        gr.SetSourceRGB(tintGrey.r, tintGrey.g, tintGrey.b);
         // Bottom face edges: (only 2 remain to be drawn)
        gr.MoveTo(UserX.X, UserX.Y);
         gr.LineTo(UserXY.X, UserXY.Y); gr.LineTo(UserY.X, UserY.Y);
         // Top face edges: (all 4 to be drawn)
        gr.MoveTo(UserZ.X, UserZ.Y);
         gr.LineTo(UserXZ.X, UserXZ.Y); gr.LineTo(UserXYZ.X, UserXYZ.Y); gr.LineTo(UserYZ.X, UserYZ.Y); gr.LineTo(UserZ.X, UserZ.Y);
        // Three vertical edges remain:
        gr.MoveTo(UserX.X,  UserX.Y);   gr.LineTo(UserXZ.X,  UserXZ.Y);
        gr.MoveTo(UserXY.X, UserXY.Y);  gr.LineTo(UserXYZ.X, UserXYZ.Y);
        gr.MoveTo(UserY.X,  UserY.Y);   gr.LineTo(UserYZ.X,  UserYZ.Y);
        gr.Stroke();
      // Place 'X Y Z O' to the right of the viewer, deployed so as to map which axis is which:
        double x0, x1, y0, y1;
         // 'O' goes halfway between the viewer and the right margin of the box.
        x0 = 0.6 * (viewLeft + viewWidth) + 0.4 *boxRight; // For the 'O' to be fraction n across the space to the right of the viewer,
                                                           // the first coefficient must be (1-n) and the second n.
        y0 = viewTop + 20;
        gr.SetSourceRGB(tintGrey.r, tintGrey.g, tintGrey.b);
        TextOnGraph(this, gr, NameFontSize, false, new Strub2(x0, y0, "O", ""));
         // X, Y and Z:
        double a = 0.15; // X, Y and Z are displaced from O by this fraction of the drawn lengths of the axes.
        Strub2[] shrub = new Strub2[3];
        x1 = x0 + a*(UserX.X - UserO.X);  y1 = y0 + a*(UserX.Y - UserO.Y);    shrub[0] = new Strub2(x1, y1, "X", "");
        x1 = x0 + a*(UserY.X - UserO.X);  y1 = y0 + a*(UserY.Y - UserO.Y);    shrub[1] = new Strub2(x1, y1, "Y", "");
        x1 = x0 + a*(UserZ.X - UserO.X);  y1 = y0 + a*(UserZ.Y - UserO.Y);    shrub[2] = new Strub2(x1, y1, "Z", "");
        gr.SetSourceRGB(tintBlack.r, tintBlack.g, tintBlack.b);
        TextOnGraph(this, gr, NameFontSize, true, shrub);
      }

     // Draw the hairlines:
      int n, hxvLen = HairsXVisible._Length(),  hyvLen = HairsYVisible._Length(),  hzvLen = 0;
      double x=0, y=0;
      if (is3D) hzvLen = HairsZVisible._Length();
      if (!NOHairsXVisible && hxvLen > 0)
      { double yBtm = boxTop + boxHeight; // Only used for 2D
        gr.NewPath();
        gr.LineWidth = 0.5;
        gr.SetSourceRGB(TintHairLines.r, TintHairLines.g, TintHairLines.b);
        gr.SetDash(new double[] {1.5, 1.5}, 0.0);
        bool setToDashes = true; // checking this saves a lot of unnec. resetting of gr.SetDash below.
        for (int i=1; i < SegsX; i++)
        { n = HairsXVisible[i % hxvLen];
          if (is3D)
          { x = UserO.X + ((double) i / (double) SegsX) * (UserX.X - UserO.X);
            y = UserO.Y + ((double) i / (double) SegsX) * (UserX.Y - UserO.Y);
            if (n == 2) // then draw two full-length hairlines:
            { if (!setToDashes) { gr.SetDash(new double[] {1.5, 1.5}, 0.0);  setToDashes = true; }
              gr.MoveTo(x, y);   gr.LineTo(x + UserY.X - UserO.X, y + UserY.Y - UserO.Y); // hairline parallel to the Y axis
              gr.MoveTo(x, y);   gr.LineTo(x, y + UserZ.Y - UserO.Y); // hairline parallel to the Z axis
              gr.Stroke ();
            }
            else if (n == 1) // just put a blob on the axis:
            { gr.Save();
              gr.NewPath(); // otherwise points are joined. (Not a tragedy, but still...)
              gr.LineWidth = 2;
              gr.SetDash(new double[] {}, 0.0);
              gr.Arc(x, y, 1, 0, PIPI);
              gr.Stroke();
              gr.Restore();
            }
          }
          else // is 2D:
          { if (n == 2)
            { if (!setToDashes) { gr.SetDash(new double[] {1.5, 1.5}, 0.0);  setToDashes = true; }
              gr.MoveTo(ticksX[i], boxTop);   gr.LineTo(ticksX[i], yBtm);
              gr.Stroke ();
            }
            else if (n == 1)
            { if (setToDashes) { gr.SetDash(new double[] {1.0}, 0.0);  setToDashes = false; }
              gr.MoveTo(ticksX[i], yBtm - StubLength);  gr.LineTo(ticksX[i], yBtm);
              gr.Stroke ();
            }
          }
        }
      }
      if (!NOHairsYVisible && hyvLen > 0)
      { double yRight = boxLeft + boxWidth;
        gr.NewPath();
        gr.LineWidth = 0.5;
        gr.SetSourceRGB(TintPerimeter.r, TintPerimeter.g, TintPerimeter.b);
        gr.SetDash(new double[] {1.5, 1.5}, 0.0);
        bool setToDashes = true; // checking this saves a lot of unnec. resetting of gr.SetDash below.
        for (int i=1; i < SegsY; i++)
        { n = HairsYVisible[i % hyvLen];
          if (is3D)
          { x = UserO.X + ((double) i / (double) SegsY) * (UserY.X - UserO.X);
            y = UserO.Y + ((double) i / (double) SegsY) * (UserY.Y - UserO.Y);
            if (n == 2) // then draw two full-length hairlines:
            { if (!setToDashes) { gr.SetDash(new double[] {1.5, 1.5}, 0.0);  setToDashes = true; }
              gr.MoveTo(x, y);   gr.LineTo(x + UserX.X - UserO.X, y + UserX.Y - UserO.Y); // hairline parallel to the X axis
              gr.MoveTo(x, y);   gr.LineTo(x, y + UserZ.Y - UserO.Y); // hairline parallel to the Z axis
              gr.Stroke();
            }
            else if (n == 1) // just put a blob on the axis:
            { gr.Save();
              gr.NewPath(); // otherwise points are joined. (Not a tragedy, but still...)
              gr.LineWidth = 2;
              gr.SetDash(new double[] {}, 0.0);
              gr.Arc(x, y, 1, 0, PIPI);
              gr.Stroke();
              gr.Restore();
            }
          }
          else // is 2D:
          { if (n == 2)
            { if (!setToDashes) { gr.SetDash(new double[] {1.5, 1.5}, 0.0);  setToDashes = true; }
              gr.MoveTo(boxLeft, ticksY[i]);  gr.LineTo(yRight, ticksY[i]);
              gr.Stroke();
            }
            else if (n == 1)
            { if (setToDashes) { gr.SetDash(new double[] {1.0}, 0.0);  setToDashes = false; }
              gr.MoveTo(boxLeft, ticksY[i]);  gr.LineTo(boxLeft + StubLength, ticksY[i]);
              gr.Stroke();
            }
          }
        }
      }

      if (is3D && (!NOHairsZVisible && hzvLen > 0))
      { gr.NewPath();
        gr.LineWidth = 0.5;
        gr.SetSourceRGB(TintPerimeter.r, TintPerimeter.g, TintPerimeter.b);
        gr.SetDash(new double[] {1.5, 1.5}, 0.0);
        bool setToDashes = true; // checking this saves a lot of unnec. resetting of gr.SetDash below.
        for (int i=1; i < SegsZ; i++)
        { n = HairsYVisible[i % hyvLen];
          x = UserO.X;
          y = UserO.Y + ((double) i / (double) SegsZ) * (UserZ.Y - UserO.Y);
          if (n == 2) // then draw two full-length hairlines:
          { if (!setToDashes) { gr.SetDash(new double[] {1.5, 1.5}, 0.0);  setToDashes = true; }
            gr.MoveTo(x, y);   gr.LineTo(x + UserX.X - UserO.X, y + UserX.Y - UserO.Y); // hairline parallel to the X axis
            gr.MoveTo(x, y);   gr.LineTo(x + UserY.X - UserO.X, y + UserY.Y - UserO.Y); // hairline parallel to the Y axis
            gr.Stroke();
          }
          else if (n == 1) // just put a blob on the axis:
          { gr.Save();
            gr.NewPath(); // otherwise points are joined. (Not a tragedy, but still...)
            gr.LineWidth = 2;
            gr.SetDash(new double[] {}, 0.0);
            gr.Arc(x, y, 1, 0, PIPI);
            gr.Stroke();
            gr.Restore();
          }
        }
      }
     // LABEL the axes, and show their SCALE:
      // X and Y Axis tick strings: (colour black)
      if (is3D) // then nothing goes in the viewer; we write stuff to the right of the viewer instead.
      { gr.SelectFontFace(FontName, FontSlant.Normal, FontWeight.Normal);
        gr.SetSourceRGB(TintAxisName.r, TintAxisName.g, TintAxisName.b);
        double leftEdge = viewLeft + viewWidth + 20,  lineY = viewTop + 100;
        // Get together the strings that are to go on the graph:
        string fromX, toX, stepX, fromY, toY, stepY, fromZ, toZ, stepZ;
        if (TickStringsX._Length() == 3) { fromX = TickStringsX[0];  toX = TickStringsX[1];  stepX = TickStringsX[2]; }
        else { fromX = LowX.ToString(ValueFormatX);  toX = HighX.ToString(ValueFormatX);   stepX = ((HighX - LowX)/SegsX).ToString(ValueFormatX); }
        if (TickStringsY._Length() == 3) { fromY = TickStringsY[0];  toY = TickStringsY[1];  stepY = TickStringsY[2]; }
        else { fromY = LowY.ToString(ValueFormatY);  toY = HighY.ToString(ValueFormatY);   stepY = ((HighY - LowY)/SegsY).ToString(ValueFormatY); }
        if (TickStringsZ._Length() == 3) { fromZ = TickStringsZ[0];  toZ = TickStringsZ[1];  stepZ = TickStringsZ[2]; }
        else { fromZ = LowZ.ToString(ValueFormatZ);  toZ = HighZ.ToString(ValueFormatZ);   stepZ = ((HighZ - LowZ)/SegsZ).ToString(ValueFormatZ); }
        // The first call to TextOnGraph(.) is to return the height of text when using font size NameFontSize, so we can set vert. line sepns.
        Duplex dupe = TextOnGraph(this, gr, NameFontSize, true, new Strub2(leftEdge, lineY, "X:  " + AxisNameX, "") );
        double NameFontSize_Ht = dupe.Y;
        // The second call to TextOnGraph(.) is to return the height of text for font size UnitsFontSize, so we can set vert. line sepns.
        gr.SetSourceRGB(TintTickStrings.r, TintTickStrings.g, TintTickStrings.b);
        Duplex drupe = TextOnGraph(this, gr, UnitsFontSize, true,
                                                  new Strub2(leftEdge, lineY + NameFontSize_Ht, "From:  " + fromX, "") );
        double UnitsFontSize_Ht = drupe.Y;
        // Now we can use TextOnGraph in the void form (by setting the bool arg. to FALSE), and print first all the stuff that is to be
        //  in font size NameFontSize, then all the stuff that is to be in font size UnitsFontSize.
        // First, stuff in font size NameFontSize:
        Strub2[] rub = new Strub2[2];
        rub[0] = new Strub2(leftEdge, lineY + 3*NameFontSize_Ht + 2*UnitsFontSize_Ht,  "Y:  " + AxisNameY, "");
        rub[1] = new Strub2(leftEdge, lineY + 6*NameFontSize_Ht + 4*UnitsFontSize_Ht,  "Z:  " + AxisNameZ, "");
        gr.SetSourceRGB(TintAxisName.r, TintAxisName.g, TintAxisName.b);
        TextOnGraph(this, gr, NameFontSize, false, rub);
        // Next, all the stuff that is to be in font size UnitsFontSize:
        Strub2[] grub = new Strub2[8];
        grub[0] = new Strub2(leftEdge, lineY +   NameFontSize_Ht +   UnitsFontSize_Ht,  "To:  "   + toX, ""); // for the X axis
        grub[1] = new Strub2(leftEdge, lineY +   NameFontSize_Ht + 2*UnitsFontSize_Ht,  "Step:  " + stepX, "");
        grub[2] = new Strub2(leftEdge, lineY + 4*NameFontSize_Ht + 2*UnitsFontSize_Ht,  "From:  " + fromY, ""); // for the Y axis
        grub[3] = new Strub2(leftEdge, lineY + 4*NameFontSize_Ht + 3*UnitsFontSize_Ht,  "To:  "   + toY, "");
        grub[4] = new Strub2(leftEdge, lineY + 4*NameFontSize_Ht + 4*UnitsFontSize_Ht,  "Step:  " + stepY, "");
        grub[5] = new Strub2(leftEdge, lineY + 7*NameFontSize_Ht + 4*UnitsFontSize_Ht,  "From:  " + fromZ, ""); // for the Z axis
        grub[6] = new Strub2(leftEdge, lineY + 7*NameFontSize_Ht + 5*UnitsFontSize_Ht,  "To:  "   + toZ, "");
        grub[7] = new Strub2(leftEdge, lineY + 7*NameFontSize_Ht + 6*UnitsFontSize_Ht,  "Step:  " + stepZ, "");
        gr.SetSourceRGB(TintTickStrings.r, TintTickStrings.g, TintTickStrings.b);
        TextOnGraph(this, gr, UnitsFontSize, false, grub);
      }
      else // 2D:
      { if (!NOTickStringsX || !NOTickStringsY)
        { gr.SetSourceRGB(TintTickStrings.r, TintTickStrings.g, TintTickStrings.b);
          gr.SelectFontFace(FontName, FontSlant.Normal, FontWeight.Normal);
          gr.SetFontSize(UnitsFontSize);
          if (!NOTickStringsX)
          { for (int i=0; i <= SegsX; i++) { gr.MoveTo(ticksX[i] - 5, boxBottom + heightUnitsX);  gr.ShowText(tickstringsX[i]); } }
          if (!NOTickStringsY)
          { for (int i=0; i <= SegsY; i++) { gr.MoveTo(boxLeft - widthUnitsY - 5.0, ticksY[i]);  gr.ShowText(tickstringsY[i]); } }
        }
        // X and Y Axis names: (same colour as graph perimeter)
        if (!NOAxisNameX || !NOAxisNameY)
        { gr.SetSourceRGB(TintAxisName.r, TintAxisName.g, TintAxisName.b);
          gr.SelectFontFace(FontName, FontSlant.Normal, FontWeight.Normal);
          gr.SetFontSize(NameFontSize);
          if (!NOAxisNameX) { gr.MoveTo(boxLeft + 20, boxBottom + heightUnitsX + heightNameX);  gr.ShowText(AxisNameX); }
          if (!NOAxisNameY)
          { gr.Save();
            gr.MoveTo(boxLeft - widthUnitsY - leftUnitsY, boxBottom - 20);
            gr.Rotate(-Math.PI/2);
            gr.ShowText(AxisNameY);
            gr.Restore();
          }
        }
      }
      // Now the plots...
      SourceValue = DateTime.Now.Ticks;
      foreach (Plot plotto in PlotsExtant) Plot.Draw(this, plotto, gr, width, height); // The static handler separates out different plot children.
    }

  public void AddPlot(params Plot[] Plots)
  { if (this.artID < 1) return; // No error message, but no action either.
    PlotsExtant.AddRange(Plots);
    ForceRedraw();
  }
  /// <summary>
  /// If a value is not a recognized plot it is simply ignored, no error being raised.
  /// If no args. are supplied, all plots of the graph's PlotsExtant will be removed.
  /// </summary>
  public void RemovePlots(params Plot[] Plots)
  { if (this.artID < 1) return; // No error message, but no action either.
    if (Plots._NorE()) { PlotsExtant.Clear();  return; }
    foreach (Plot plotto in Plots)
    { while (true) // this loop allows for duplications
      { int n = PlotsExtant.IndexOf(plotto);
        if (n == -1) break;
        PlotsExtant.RemoveAt(n);
      }
    }
    ForceRedraw();
  }
  /// <summary>
  /// If a value is not a recognized plot it is simply ignored, no error being raised.
  /// If no args. are supplied, nothing happens. (Use method 'RemovePlots()' if you want to remove all plots.)
  /// This method would not remove duplicated plots.
  /// </summary>
  public void RemovePlotsByID(params int[] PlotIDs)
  { if (this.artID < 1) return; // No error message, but no action either.
    if (PlotIDs._NorE()) return;
    foreach (int plid in PlotIDs)
    { int foundAt = -1;
      for (int i=0; i < PlotsExtant.Count; i++)
      { if (PlotsExtant[i].ArtID == plid) { foundAt = i;  break; }  }
      if (foundAt >= 0) PlotsExtant.RemoveAt(foundAt);
    }
    ForceRedraw();
  }

  /// <summary>
  /// <para>Finds the extreme axis values within the whole collection of plots.</para>
  /// <para>If 'are3DPlots' true, returns an array of size 6: [lowestX] [highestX] [lowestY] [highestY] [lowestZ] [highestZ].
  ///  Otherwise returns an array of size 4.</para>
  /// <para>If a member of Plottery is 2D while are3DPlots is TRUE, or vice versa, that plot is ignored. </para>
  /// <para>If Plottery is null or empty or contains no plots of the required dimensionality, returns an empty array.</para>
  public static double[] DevelopExtremes(Plot[] Plottery, bool are3DPlots)
  { if (Plottery._Length() < 1) return new double[0];
    double Xlow, Ylow, Zlow, Xhigh, Yhigh, Zhigh; // will eventually be the returned values.
    Xlow  = Ylow  = Zlow  = double.MaxValue;
    Xhigh = Yhigh = Zhigh = double.MinValue;
    double xlo, xhi, ylo, yhi, zlo, zhi;
    foreach (Plot pluto in Plottery)
    { if (pluto.Is3D != are3DPlots) continue;
      pluto.ExtremeValues('X', out xlo, out xhi);
      if (xlo < Xlow) Xlow = xlo;   if (xhi > Xhigh) Xhigh = xhi;
      pluto.ExtremeValues('Y', out ylo, out yhi);
      if (ylo < Ylow) Ylow = ylo;   if (yhi > Yhigh) Yhigh = yhi;
      if (pluto.Is3D)
      { pluto.ExtremeValues('Z', out zlo, out zhi);
        if (zlo < Zlow) Zlow = zlo;   if (zhi > Zhigh) Zhigh = zhi;
      }
    }
    if (Xlow > Xhigh) return new double[0]; // can only happen if no plots of the required type (2D or 3D) were present.
    else if (are3DPlots) return new double[] { Xlow, Xhigh, Ylow, Yhigh, Zlow, Zhigh };
    else return new double[] { Xlow, Xhigh, Ylow, Yhigh };
  }

  /// <summary>
  /// <para>If board not identified, returns NULL. Otherwise elements are as follows:</para>
  /// <para>ACTUAL BOARD dimensions: [0] = width, [1] = height, [2] = LblHeader width, [3] = LblHeader height,
  ///  [4] = drawing area width, [5] = drawing area height, [6] = TVBelow width ( or 0, if no TVBelow exists), [7] = TVBelow height (or 0).</para>
  /// <para>ORIG. REQUESTED BOARD dimensions: [8] = width, [9] = height, [10] = eboxHeader height, [11] = drawing area height,
  ///  [12] = bool: 1 if TVBelow was requested, 0 if not. [13], [14] = window's Left and Top on the screen.
  ///  Elements [15] to [19] are currently unused, but are reserved for use by
  ///  Board.SizesOf(.), which is called in this method.</para>
  /// <para>GRAPH perimeter dimensions: [20] = width, [21] = height. (Elements 11 to 19 are not used.)</para>
  /// </summary>
  public int[] SizingParameters()
  { List<int> sizery = new List<int>(22);
    sizery.AddRange(Board.SizesOf(owningBoard)); // The Board method returns an array of size 20.
    sizery.Add( (int) boxWidth); sizery.Add( (int) boxHeight);
    return sizery.ToArray();
  }

/// <summary>
/// Returns an array consisting of the following colours:
///  [0]: Graph box (i.e. plotting surface);  [1]: graph hairlines;    [2]: the border of the graph box;
///  [3]: Outside the graph box (where axis scaling info. is printed); [4]: text of unit values at hairlines;
///  [5]: text of axis name (e.g. "velocity (kms.)".
/// </summary>
  public Gdk.Color[] GetColours()     //       0             1              2                3             4              5
  { Gdk.Color[] result = new Gdk.Color[] { ClrGraphBgd, ClrHairLines, ClrPerimeter, ClrMarginBgd, ClrTickStrings, ClrAxisName };
    return result;
  }

/// <summary>
/// Void. Sets six graph colours, if the supplied array has exactly length 6; otherwise it does nothing. The elements must be:
///  [0]: Graph box (i.e. plotting surface);  [1]: graph hairlines;    [2]: the border of the graph box;
///  [3]: Outside the graph box (where axis scaling info. is printed); [4]: text of unit values at hairlines;
///  [5]: text of axis name (e.g. "velocity (kms.)".
///  The boolean array: if ApplySetting[i] is FALSE, NewColours[i] will not be applied.
/// </summary>
  public void SetColours(Gdk.Color[] NewColours, bool[] ApplySetting)
  { if (NewColours.Length != 6) return;
    if (ApplySetting[0]) { ClrGraphBgd    =  NewColours[0];   TintGraphBgd  = new Tint(ClrGraphBgd); }
    if (ApplySetting[1]) { ClrHairLines   =  NewColours[1];   TintHairLines = new Tint(ClrHairLines); }
    if (ApplySetting[2]) { ClrPerimeter   =  NewColours[2];   TintPerimeter = new Tint(ClrPerimeter); }
    if (ApplySetting[3]) { ClrMarginBgd   =  NewColours[3];   TintMarginBgd = new Tint(ClrMarginBgd); }
    if (ApplySetting[4]) { ClrTickStrings =  NewColours[4];   TintTickStrings = new Tint(ClrTickStrings); }
    if (ApplySetting[5]) { ClrAxisName    =  NewColours[5];   TintAxisName  = new Tint(ClrAxisName); }

    ForceRedraw();
  }
  /// <summary>
  /// <para>Used to rationalize axis limits where OldLow and OldHigh are simply the lowest and highest values in the array of plot points.
  /// Returns (New low value, New high value, New no. of segments (between 4 and 7) ).</para>
  /// <para>You may prefer to use JM.ImproveRange(double OrigXLo, double OrigXHi, params int[] LoHiSegments), which
  ///  gives tighter graph margins at the cost of less rounded values (e.g. '11, 6, 1, -4...' instead of '20, 10, 0, 10...')
  ///  It also allows you to specify your own range of trial nos. of segments.</para>
  /// </summary>
  public static Triplex RationalizedGraphLimits(double OldLow, double OldHigh)
  { Triplex result = new Triplex();
    if (OldLow == OldHigh) // fudge their values, to avoid a '-NaN' below:
    { if (OldLow == 0){OldLow = -1.0;  OldHigh = 1.0; }
      else if (OldLow > 0) {OldHigh = OldLow * 1.5;  OldLow *= 0.5; }
      else {OldHigh = OldLow * 0.5;  OldLow *= 1.5; } // neg. nos.
    }
    // Develop a suitable lower limit, based on the order of the difference:
    double diff = OldHigh - OldLow, logdiff = Math.Log10(diff);
    double logdiffint = Math.Floor(logdiff);
    double lo1 = Math.Floor(OldLow * Math.Pow(10, -logdiffint));
    double newlo = lo1 * Math.Pow(10, logdiffint);
   // Adjust the difference upwards (if nec.) to an allowed difference:
    double adjdiff = Math.Round(diff * Math.Pow(10,-logdiffint),2);
    double[] pegs = new double[]
          {1.0, 1.2, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 5.0, 6.0, 7.0, 8.0, 10.0};
    double newadjdiff = 10.0;  int pegindex = pegs.Length-1;
    for (int ii = 0; ii < pegs.Length-1; ii++)
    { if (pegs[ii] >= adjdiff)
      { newadjdiff = pegs[ii]; pegindex = ii; break; } }
    double newdiff = newadjdiff * Math.Pow(10, logdiffint);
    // Check that this newlo and newdiff give a newhi inclusive of hi;
    //  if not, increase difference.
    double newhi = newlo + newdiff;
    while (newhi < OldHigh)
    { pegindex++;
      if (pegindex >= pegs.Length) break; // = failure of algorithm, alas.
      newadjdiff = pegs[pegindex];
      newdiff = newadjdiff * Math.Pow(10, logdiffint);
      newhi = newlo + newdiff;
    }
    result.X = newlo;  result.Y = newhi;
   // Set the no. of segments:
    double[] segs = new double[]
        { 5.0, 6.0, 6.0, 4.0, 5.0, 6.0, 7.0, 4.0, 5.0, 6.0, 7.0, 4.0, 5.0 };
    if (pegindex == pegs.Length)result.Z = 5.0; // algorithm failed, so just pick 5
    else result.Z = segs[pegindex];
    return result;
  }

/// <summary>
/// All arguments reflect instance fields of class Graph; the instance fields all prefix X, Y or Z to the arg. names below.
/// Note that TickStrings itself is not altered; it is simply used to set the protected field tickstrings[] which is what is actually displayed.
/// </summary>
  public static void ProcessTickStrings(ref string[] TickStrings, ref string[] tickstrings, int[] TickStringsVisible,  double LoVal, double HiVal,
                  int Segs, string Suffix, bool SuppressValueIfOne, bool SuppressSuffixIfZero, double FudgeFactor, string ValueFormat)
  {// Set up the visibility array:
    int n = 0, tsVisLen = TickStringsVisible._Length();
    bool[] tsVisible = new bool[Segs+1];
    bool makeAllVisible = false;
    if (tsVisLen < 2) makeAllVisible = true;
    else
    { for (int i=0; i < tsVisLen; i++)
      { n += TickStringsVisible[i];
        if (n < 0 || (n == 0 && i > 0)) { makeAllVisible = true;  break; }
        else if (n > Segs) break; // Note, by the way, that deliberately setting TSV[0] > Segs results in no tick strings being visible.
         tsVisible[n] = true;
      }
      if (!makeAllVisible && n < Segs)
      { int diff = TickStringsVisible[tsVisLen-1];
        while (true)
        { n += diff;  if (n > Segs) break;
           tsVisible[n] = true;
        }
      }
    }
    if (makeAllVisible) { for (int i=0; i <= Segs; i++) tsVisible[i] = true; }
   // Set up the output strings array:
    int tsLen = TickStrings._Length();  if (tsLen == -1) tsLen = 0;
    tickstrings = new string[Segs+1];
    int mino = Math.Min(tsLen, Segs+1);
    for (int i=0; i < mino; i++) { if (tsVisible[i]) tickstrings[i] = TickStrings[i];  else tickstrings[i] = ""; }
    // Generate values for unfilled positions:
    double val, span = HiVal - LoVal;   string unit;
    for (int i = mino; i <= Segs; i++)
    { unit = "";
      if (tsVisible[i])
      { if (i == Segs) val = HiVal;   else val = LoVal + span * i / Segs;
        val *= FudgeFactor;
        unit = val.ToString(ValueFormat);
        if (SuppressValueIfOne) // a rare event, hence the above setting, which is reset on rare occasions below.
        { if (val == 1.0) unit = ""; else if (val == -1.0) unit = "-"; }
        if (!SuppressSuffixIfZero || val != 0.0) unit += Suffix;
      }
      tickstrings[i] = unit;
    }
  }

  public static void Magnify(int BoardID, int GraphID, double H_Magnification, double V_Magnification)
  { Graph G = Board.GetFocussedGraph(BoardID);   if (G == null) return;
    double centreX = (G.LowX + G.HighX) / 2.0,  centreY = (G.LowY + G.HighY) / 2.0;
    double newHalfSpanX = (G.HighX - G.LowX) / (2.0 * H_Magnification),
           newHalfSpanY = (G.HighY - G.LowY) / (2.0 * V_Magnification);
    G.LowX = centreX - newHalfSpanX;    G.HighX = centreX + newHalfSpanX;
    G.LowY = centreY - newHalfSpanY;    G.HighY = centreY + newHalfSpanY;
    if (G.is3D) // then all dimensions should increment by the same value, H_Magnification. (Calling code should set "V_M." = "H_M." for 3D.)
    { double centreZ = (G.LowZ + G.HighZ) / 2.0;
      double newHalfSpanZ = (G.HighZ - G.LowZ) / (2.0 * H_Magnification);
      G.LowZ = centreZ - newHalfSpanZ;    G.HighZ = centreZ + newHalfSpanZ;
    }
    G.ForceRedraw();
  }

  public static void OriginalScale(int BoardID, int GraphID)
  { Graph G = Board.GetFocussedGraph(BoardID);   if (G == null) return;
    G.LowX = G.OrigLowX;    G.HighX = G.OrigHighX;
    G.LowY = G.OrigLowY;    G.HighY = G.OrigHighY;
    G.LowZ = G.OrigLowZ;    G.HighZ = G.OrigHighZ;
    G.SegsX = G.OrigSegsX;  G.SegsY = G.OrigSegsY;  G.SegsZ = G.OrigSegsZ;
    G.ForceRedraw();
  }

  public static void MoveViewPoint(int BoardID, int GraphID, double rightFactor, double upFactor)
  { Graph G = Board.GetFocussedGraph(BoardID);   if (G == null) return;
    double ang, PIby2 = 2*Math.PI;
    if (G.is3D)
    { ang = rightFactor * BasicMoveAngle;
      if (ang > PIby2) ang -= PIby2;  else if (ang < -PIby2) ang += PIby2;
      G.Ascension -= ang;
      ang = upFactor * BasicMoveAngle;
      if (ang > PIby2) ang -= PIby2;  else if (ang < -PIby2) ang += PIby2;
      G.Declination -= ang;
    }
    else // 2D:
    { double spanX = G.HighX - G.LowX,   spanY = G.HighY - G.LowY;
      G.LowX += spanX * rightFactor;   G.HighX += spanX * rightFactor;
      G.LowY += spanY * upFactor;      G.HighY += spanY * upFactor;
    }
    G.ForceRedraw();
  }
  /// <summary>
  /// Converts from pixel coords to user coords. If the graph cannot be identified, returns double.NaN.
  /// </summary>
  public static void ToUserCoords(Graph G, double pixelX, double pixelY, out double userX, out double userY)
  { if (G == null) { userX = userY = double.NaN;  return; }
    double spanX = G.HighX - G.LowX,   spanY = G.HighY - G.LowY;
    userX = G.LowX + (pixelX - G.boxLeft) * spanX / G.boxWidth;
    userY = G.LowY + (G.boxBottom - pixelY) * spanY / G.boxHeight;
  }
/// <summary>
/// <para>Returns a complete copy of graph with identifier GraphID. If UseStartUpSizing TRUE, uses the original board's start-up
///  size, and also in the case of 3D graphs, the original graph's start-up ascension and declination.</para>
/// <para>If GraphID cannot be identified, returns NULL.</para>
/// </summary>
public static Graph CopyOf(int GraphID, bool UseStartUpSizing)
{ Graph OldG, result = null;
  Trio troll = Board.GetBoardOfGraph(GraphID, out OldG);
  if (OldG == null || troll.X < 1) return null;
  int newBoard = Board.CopyOf(troll.X, UseStartUpSizing);
  if (OldG.is3D)
  { result = new Graph(newBoard, OldG.LowX, OldG.HighX, OldG.SegsX, OldG.LowY, OldG.HighY, OldG.SegsY, OldG.LowZ, OldG.HighZ, OldG.SegsZ); }
  else
  { result = new Graph(newBoard, OldG.LowX, OldG.HighX, OldG.SegsX, OldG.LowY, OldG.HighY, OldG.SegsY); }
  string windowTitle, heading, origDescription, description; // *** Currently we don't use 'origDescription'.
  bool outcome = Board.GetTexts(troll.X, out windowTitle, out heading, out origDescription, out description);
  if (outcome) Board.Texts(newBoard, windowTitle, heading, description, false); // false = no cursor visible below the graph.
  result.OrigLowX = OldG.OrigLowX;  result.OrigHighX = OldG.OrigHighX;  result.OrigSegsX = OldG.OrigSegsX;
  result.OrigLowY = OldG.OrigLowY;  result.OrigHighY = OldG.OrigHighY;  result.OrigSegsY = OldG.OrigSegsY;
  result.OrigLowZ = OldG.OrigLowZ;  result.OrigHighZ = OldG.OrigHighZ;  result.OrigSegsZ = OldG.OrigSegsZ;
  result.AxisNameX = OldG.AxisNameX;  result.AxisNameY = OldG.AxisNameY;   result.AxisNameZ = OldG.AxisNameZ;
  result.NOAxisNameX = OldG.NOAxisNameX;  result.NOAxisNameY = OldG.NOAxisNameY;   result.NOAxisNameZ = OldG.NOAxisNameZ;
  result.TickStringsX = OldG.TickStringsX;  result.TickStringsY = OldG.TickStringsY;  result.TickStringsZ = OldG.TickStringsZ;
  result.TickStringsXVisible = OldG.TickStringsXVisible;
  result.TickStringsYVisible = OldG.TickStringsYVisible;
  result.NOTickStringsX = OldG.NOTickStringsX;    result.NOTickStringsY = OldG.NOTickStringsY;
  result.HairsXVisible = OldG.HairsXVisible;
  result.HairsYVisible = OldG.HairsYVisible;
  result.HairsZVisible = OldG.HairsZVisible;
  result.NOHairsXVisible = OldG.NOHairsXVisible;
  result.NOHairsYVisible = OldG.NOHairsYVisible;
  result.NOHairsZVisible = OldG.NOHairsZVisible;
  result.GapsX = OldG.GapsX;   result.GapsY = OldG.GapsY;   result.GapsZ = OldG.GapsZ;
  result.FudgeFactorX = OldG.FudgeFactorX;   result.FudgeFactorY = OldG.FudgeFactorY;   result.FudgeFactorZ = OldG.FudgeFactorZ;
  result.SuffixX = OldG.SuffixX;             result.SuffixY = OldG.SuffixY;             result.SuffixZ = OldG.SuffixZ;
  result.SuppressSuffixIfZeroX = OldG.SuppressSuffixIfZeroX;
  result.SuppressSuffixIfZeroY = OldG.SuppressSuffixIfZeroY;
  result.SuppressSuffixIfZeroZ = OldG.SuppressSuffixIfZeroZ;
  result.SuppressValueIfOneX = OldG.SuppressValueIfOneX;
  result.SuppressValueIfOneY = OldG.SuppressValueIfOneY;
  result.SuppressValueIfOneZ = OldG.SuppressValueIfOneZ;
  if (!UseStartUpSizing)
  { result.Declination = OldG.Declination;   result.Ascension = OldG.Ascension; }
  result.ClrGraphBgd = OldG.ClrGraphBgd;   result.ClrMarginBgd = OldG.ClrMarginBgd;  result.ClrPerimeter = OldG.ClrPerimeter;
  result.ClrHairLines = OldG.ClrHairLines; result.ClrTickStrings = OldG.ClrTickStrings;  result.ClrAxisName = OldG.ClrAxisName;
  result.FontName = OldG.FontName;  result.NameFontSize = OldG.NameFontSize;   result.UnitsFontSize = OldG.UnitsFontSize;
  result.ValueFormatX = OldG.ValueFormatX;
  result.ValueFormatY = OldG.ValueFormatY;
  result.ValueFormatZ = OldG.ValueFormatZ;
  return result;
}

} // End of class Graph

//===================================================

  /// <summary>
  /// Plot points and optionally join them with straight lines.
  /// </summary>
  public class Plot : Art                           //plot
  {
  // STATIC FIELDS:
    public static char DefPtShape = '.';
    public static double DefPtWidth = 3.0;
    private static Tint DefPtTint = new Tint(0.0, 0.0, 0.0);
    public static Gdk.Color DefPtClr { get { return Tint.GDKClr(DefPtTint); }  set { DefPtTint = new Tint(value); } }

    public static char DefLnShape = '_';
    public static double DefLnWidth = 1.0;
    private static Tint DefLnTint = new Tint(0.0, 0.0, 0.0);
    public static Gdk.Color DefLnClr { get { return Tint.GDKClr(DefLnTint); }  set { DefLnTint = new Tint(value); } }

  // INSTANCE FIELDS:
  //   PROTOCOL: Whenever a protected field is altered, a record of the time of alteration will be kept in public instance field PlotRevisionTime.
  //    Drawing methods will have a record of their last call on the plot, and will only cause recalculation of plot's derived fields if
  //    PlotRevisionTime is later. (A user could obviously force redrawing by setting this to e.g. 0, but why would you? User-adjustment to
  //     a later time would stop recalculation till the next change of a property; but this would have no visible effect on anything.)
    public long PlotRevisionTime = 1; // It has to start higher than the next field, to force the first calculation of parameters.
    public long LastPlotRecalcn = 0;
    protected bool is3D; // Value is only ever set in the 3D constructors below.
      public bool Is3D { get { return is3D; } }

   // POINT-related parameters:
    protected double[] XX, YY, ZZ; // Point coordinates in user space. They can have diff. lengths. The constructor initializes these;
          // the user can alter them either by the property below (for complete replacement of XX or YY) or by static method ReplaceCoords
          // (allowing alteration over a range, e.g. for a single point).
      public double[] XCoords
      { get { return (double[]) XX.Clone(); } set { XX = (double[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      public double[] YCoords
      { get { return (double[]) YY.Clone(); } set { YY = (double[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      public double[] ZCoords
      { get { return (double[]) ZZ.Clone(); } set { ZZ = (double[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }

    protected double[] XPx, YPx; // Point coordinates in pixels, relative to the whole drawing area, internally created and forced to equal length.
    // The key to what shape, width and colour is drawn is PtWeight (length >= 1). PtWeight[i] holds the index in PtShape, PtWidth and PtColour
    //  to use for drawing point (XPx[i], YPx[i]); neg. value = don't plot the point. For the same shape, width and colour throughout, PtWeight has
    //  all values  = n (typically 0), and Pt.. arrays have length (at least) n+1, and Pt..[n] hold the shape, width and colour to use.
      public double[] Xpixels { get { return (double[]) XPx.Clone(); } }
      public double[] Ypixels { get { return (double[]) YPx.Clone(); } }
    protected int[] ptWeight; // As above. Value -1 = don't plot the point. Length must always be at least 1.
      public int[] PtWeight
      { get { return (int[]) ptWeight.Clone(); }
        set { if (value != null &&  value.Length > 0) { ptWeight = (int[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      }
    protected char[] ptShape; // Length >= n+1, where n is the highest value in PtWeight.
      public char[] PtShape
      { get { return (char[]) ptShape.Clone(); }
        set { if (value != null &&  value.Length > 0) ptShape = (char[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; }
      }
    protected double[] ptWidth; // Length >= n+1, where n is the highest value in PtWeight.
      public double[] PtWidth
      { get { return (double[]) ptWidth.Clone(); }
        set { if (value != null &&  value.Length > 0) ptWidth = (double[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; }
      }
    protected Tint[] ptTintArray; // Length >= n+1, where n is the highest value in PtWeight.
      public Gdk.Color[] PtClrArray
      { get { return Tint.GDKClrArray(ptTintArray); }
        set
        { if (value != null &&  value.Length > 0)
          { ptTintArray = Tint.TintArray(value);  PlotRevisionTime = DateTime.Now.Ticks; }
        }
      }

    public string[] texts; // Any length or NULL; if to be used (as indicated by PtShape = '$'), Texts[i mod Texts.Length] will be accessed.
      public string[] Texts
      { get { return (string[]) texts.Clone(); } // Yes, Clone() does copy strings, not just pointers to them; 'texts' and 'Texts' are independent.
        set { if (value == null) texts = null; else texts = (string[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; }
      }

   // LINE-related parameters:
    // The key to what shape, width and colour is drawn is LnWeight (length >= 1). LnWeight[i] holds the index in LnShape, LnWidth and LnColour
    //  to use for drawing point (XPx[i], YPx[i]); neg. value = don't plot the point. For the same shape, width and colour throughout, LnWeight has
    //  all values  = n (typically 0), and Ln.. arrays have length (at least) n+1, and Ln..[n] hold the shape, width and colour to use.
    protected int[] lnWeight; // As above. Value -1 = don't joint to the next point.
      public int[] LnWeight
      { get { return (int[]) lnWeight.Clone(); }
        set { int len = value._Length();  if (len > 0) { lnWeight = (int[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      }
    protected char[] lnShape; // Length >= n+1, where n is the highest value in lnWeight.
      public char[] LnShape
      { get { return lnShape._Copy(); }
        set { int len = value._Length();  if (len > 0) lnShape = value._Copy();  PlotRevisionTime = DateTime.Now.Ticks; }
      }
    protected double[] lnWidth; // Length >= n+1, where n is the highest value in PtWeight.
      public double[] LnWidth
      { get { return lnWidth._Copy(); }
        set { int len = value._Length();  if (len > 0) lnWidth = value._Copy();  PlotRevisionTime = DateTime.Now.Ticks; }
      }
    protected Tint[] lnTintArray; // Length >= n+1, where n is the highest value in PtWeight.
      public Gdk.Color[] LnClrArray
      { get { return Tint.GDKClrArray(lnTintArray); }
        set { int len = value._Length();  if (len > 0) lnTintArray = Tint.TintArray(value);  PlotRevisionTime = DateTime.Now.Ticks; }
      }

    protected bool closeLoop; // If TRUE, the last point in XPx will be connected to the first point.
      public bool CloseLoop { get { return closeLoop; } set { closeLoop = value;  PlotRevisionTime = DateTime.Now.Ticks; } }
    // The following is only used if the point shape supplied to Plot.Draw(.) is '?'; in that case, the user must first have
    //   assigned a legal value to this. (If it remains null despite the '?', it is ignored, and no point is drawn.)
    // For examples of dash pattern, see the homemade ones in method Plot.Join(.); values are { ON pixels, OFF pixels, ON pixels.. }.
    public double[] dashPattern;
      public double[] DashPattern
      { get { return (double[]) dashPattern.Clone(); }
        set { if (value != null &&  value.Length > 0) dashPattern = (double[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; }
      }
    public string FontName; // for use where field 'texts' has values.

//----------------------------------------------------------
//   CONSTRUCTORS
//----------------------------------------------------------
    // The innermost constructor - not public - is available for any child class.
    protected Plot() : base()
    { artID += Board.PlotIDBase;
      is3D = false; // Assume 2D till proved otherwise.
      XX = null;  YY = null;   ZZ = null;
      XPx = null; YPx = null;
      lnWeight = new int[] { 0, 0, 0, 0, 0 }; // 5 values to cut down no. of rotational resets.
      lnShape = new char[] { DefLnShape };
      lnWidth = new double[] { DefLnWidth };
      lnTintArray = new Tint[] { DefLnTint };

      closeLoop = false;
      dashPattern = null; // DrawPlot(.) will not invoke dash patterning, if this array is null.
      FontName = Graph.DefFontName;
    }
    // Constructor - still not public - leaves point coord arrays as NULL, but sets default values for point and line characteristics.
    protected Plot(int dummy) : this()
    { ptWeight = new int[] { 0, 0, 0, 0, 0 };
      ptShape = new char[] { DefPtShape };
      ptWidth = new double[] { DefPtWidth };
      ptTintArray = new Tint[] { DefPtTint };

      texts = null;
    }
    // This constructor sets XX and YY (by copying). 'is3D' remains FALSE (as set by the base constructor). If either array is null or empty,
    //  an exception is thrown; but they may have unequal length.
    public Plot(double[] XCoords, double[] YCoords) : this(0)
    { if (XCoords._NorE() || YCoords._NorE()) throw new Exception("The Plot constructor cannot be given null or empty arrays");
      XX = XCoords._Copy();    YY = YCoords._Copy();
    }
    // This constructor sets XX, YY and ZZ (by copying), and in the process sets 'is3D' to TRUE. If either array is null or empty,
    //  an exception is thrown; but they may have unequal length.
    // Field .is3D is set to TRUE (from the default FALSE) by a call to this constructor.
    public Plot(double[] XCoords, double[] YCoords, double[] ZCoords) : this(0)
    { if (XCoords._NorE() || YCoords._NorE()) throw new Exception("The Plot constructor cannot be given null or empty arrays");
      XX = XCoords._Copy();    YY = YCoords._Copy();    ZZ = ZCoords._Copy();
      is3D = true;
    }
    // The first two arrays must be non-null, non-empty. If any remaining arrays are null or empty they will be ignored and the constructor's
    //  defaults left in place. Note that no constructor sets PtWeight and LnWeight, which default to using the first element in each Pt/Ln array.
    public Plot(double[] XCoords, double[] YCoords, char[] PtShape_, double[] PtWidth_, Gdk.Color[] PtClr_,
                                                 char[] LnShape_, double[] LnWidth_, Gdk.Color[] LnClr_ ) : this(XCoords, YCoords)
    { if (!PtShape_._NorE()) ptShape = (char[])    PtShape_.Clone();
      if (!PtWidth_._NorE()) ptWidth = (double[])  PtWidth_.Clone();
      if (!PtClr_._NorE())   ptTintArray = Tint.TintArray(PtClr_);

      if (!LnShape_._NorE()) lnShape = (char[])    LnShape_.Clone();
      if (!LnWidth_._NorE()) lnWidth = (double[])  LnWidth_.Clone();
      if (!LnClr_._NorE())   lnTintArray = Tint.TintArray(LnClr_);
    }
    // The first two arrays must be non-null, non-empty. If any remaining arrays are null or empty they will be ignored and the constructor's
    //  defaults left in place. Note that no constructor sets PtWeight and LnWeight, which default to using the first element in each Pt/Ln array.
    // Field .is3D is set to TRUE (from the default FALSE) by a call to this constructor. Thhe ONLY WAY that calling code can set .is3D to TRUE
    // is by this or the constructor which it calls.
    public Plot(double[] XCoords, double[] YCoords, double[] ZCoords, char[] PtShape_, double[] PtWidth_, Gdk.Color[] PtClr_,
                                                 char[] LnShape_, double[] LnWidth_, Gdk.Color[] LnClr_ ) : this(XCoords, YCoords, ZCoords)
    { if (!PtShape_._NorE()) ptShape = (char[])    PtShape_.Clone();
      if (!PtWidth_._NorE()) ptWidth = (double[])  PtWidth_.Clone();
      if (!PtClr_._NorE())   ptTintArray = Tint.TintArray(PtClr_);

      if (!LnShape_._NorE()) lnShape = (char[])    LnShape_.Clone();
      if (!LnWidth_._NorE()) lnWidth = (double[])  LnWidth_.Clone();
      if (!LnClr_._NorE())   lnTintArray = Tint.TintArray(LnClr_);
    }
//-----------------------------
//  DRAWING PLOTS: Problem - we have to distinguish between plot objects and objects from child classes of plot - which in turn may
//   call 'DrawPlot' below. The solution chosen is to use a static handler:
    public static void Draw(Graph ThisGraph, Plot ThisPlot, Cairo.Context gr, double width, double height)
    { if      (ThisPlot is Mesh)  (ThisPlot as Mesh).DrawMesh(ThisGraph, gr, width, height);
      else if (ThisPlot is Shape) (ThisPlot as Shape).DrawShape(ThisGraph, gr, width, height);
      // *** Later put other drawers of plot children here
      else ThisPlot.DrawPlot(ThisGraph, gr, width, height);
    }
    /// <summary>
    /// Although this method is public, it would be a daunting task for external code to call it directly, as it would be hard to
    /// work up a Cairo.Context object for its first argument. It is intended to be called from the above static method 'Draw' and
    /// from child classes (notably class Mesh).
    /// </summary>
    public void DrawPlot(Graph ThisGraph, Cairo.Context gr, double width, double height)
    { if (XX._NorE() || YY._NorE() || (is3D && ZZ._NorE())) return;
      double bxLeft = ThisGraph.BoxLeft, bxRight = ThisGraph.BoxRight, bxBottom = ThisGraph.BoxBottom, bxTop = ThisGraph.BoxTop;
      double bxWidth = ThisGraph.BoxWidth, bxHeight = ThisGraph.BoxHeight;
     // DEVELOP PIXEL COORDINATES for points:
      if (is3D) Projections(ThisGraph, this);
      else // is 2D:
      // Set local variables to graph base variables, as they will be referenced repeatedly in loops:
      if ( (this is Mesh) || ThisGraph.LastGraphRecalcn > LastPlotRecalcn || PlotRevisionTime > LastPlotRecalcn)
                // Mesh keeps re-calling this method, even though ThisGraph itself does not change  ( --> .LastGraphRecalcn unchanged).
      { double LoX = ThisGraph.LowX,  HiX = ThisGraph.HighX,  LoY = ThisGraph.LowY,  HiY = ThisGraph.HighY;
        double SpanX = HiX - LoX,   SpanY = HiY - LoY;
       // Build up shortfall arrays:
        int XLen = this.XX.Length,   YLen = YY.Length;
        int XYLen = Math.Max(XLen, YLen),   ix = -1,  iy = -1;
        XPx = new double[XYLen];     YPx = new double[XYLen];
        double x, y, xx, yy, toobig = bxWidth * bxHeight + 5; // If we tie XPx and YPx values to +/- this, then in the worst case, a line from
          // a point (x1,y1) on the graph to a point e.g. (x2, truncated y2) is only 1/5 of a pixel from the line to the untruncated point.
          // By experimentation this was found necessary, as Cairo inverts the sign of an oversized coord. if it is very much bigger than that.
        double toosmall = -toobig;
        for (int i=0; i < XYLen; i++)
        { ix++;  if (ix == XLen) ix = 0;     iy++;  if (iy == YLen) iy = 0;
          xx = XX[ix];  yy = YY[iy];
          x = bxLeft   + (xx - LoX) * bxWidth  / SpanX;
          if (x < toosmall) x = toosmall;  else if (x > toobig) x = toobig;
          y = bxBottom - (yy - LoY) * bxHeight / SpanY;
          if (y < toosmall) y = toosmall;  else if (y > toobig) y = toobig;
          XPx[i] = x;   YPx[i] = y;
        }
        LastPlotRecalcn = DateTime.Now.Ticks;
      }
    // DRAW LINES:
      // Lines are not allowed to transgress the barriers of the graph, unlike points, as the external part adds no usual viewing
      // information and produces a visually untidy image.
      gr.Save();
      gr.MoveTo(bxLeft, bxTop);      gr.LineTo(bxRight, bxTop);
      gr.LineTo(bxRight, bxBottom);  gr.LineTo(bxLeft, bxBottom);
      gr.ClosePath();
      gr.Clip();
      // Now join the points within this reduced plotting region:
      Join(gr, XPx, YPx, false, DefPtTint); // Last is a dummy, as we will not be filling.
      gr.Restore();
    // DRAW POINTS:
      // Define a path for gr.Clip(), which delimits the region in which points may be drawn. Allow a little overlap beyond graph edges,
      //  so that complex point shapes may be recognized for points lying right on the graph edge.
      // (Method Spot() will not plot any point of which the centre is beyond the graph edge.)###### MAKE THAT HAPPEN
      gr.Save();
      double overhang = 5; // Plot up to a width of 10 pixels will be displayed; this covers all commonly chosen point widths,
                           //  without being visually disturbing.
      gr.MoveTo(bxLeft-overhang, bxTop-overhang);      gr.LineTo(bxRight+overhang, bxTop-overhang);
      gr.LineTo(bxRight+overhang, bxBottom+overhang);  gr.LineTo(bxLeft-overhang, bxBottom+overhang);
      gr.ClosePath();
      gr.Clip();
      // Now draw the points within this reduced plotting region:
      if (ptShape[0] != '\u0000') // the cue for not plotting. (So no point in using it with 'point family', as no other pt. shape will ever be reached.)
      { Spot(gr); }
      gr.Restore(); // This abolishes the clip region, so that the full drawing area is available for plotting again.
    }

//---------------------------------------------------------------
/// <summary>
/// <para>Input PIXEL points (XPx[i], YPx[i]) are plotted, except those for which the corresponding value PtWeight[i] is -1.</para>
/// <para>Pixel values are referred to the rectangle of the Board's drawing area, not to the graph perimeter. It is up to calling code
///  to do the conversion.</para>
/// <para>Calling code MUST ensure that the first two array args. have the same nonzero length, or else no plotting happens.</para>
/// <para> POINT SHAPES: Valid Shape values are:  '.' (solid circle), 'o'[small letter] (empty circle), 'O' [capital] (thicker empty circle),
/// '[' (solid square), ']' (empty square), 'x' (cross), 'X' (cross with lines 2 pixels wide), '+' (plus sign), '#' (plus sign, lines 2 pixels wide),
/// '$' (use string from Texts).  Anything else results in no plot. Argument Texts is only accessed if the Shape value is '$'.</para>
/// <para> TEXT AT POINTS: The first letter of Texts[i] is centred at (XPx[i], YPx[i]). If Texts[i] is null or empty or beyond the end of Texts,
///  no plot occurs.</para>
/// <para>Where text is being used, the font should have been set before this method was called (i.e. gr.SelectFontFace(.). The font size will
///  be the value in PtWidth.</para>
/// </summary>
  public void Spot(Cairo.Context gr)
  { int len = this.XPx._Length();
    if (len < 1 || YPx._Length() != len) return; // No points will be plotted, but no error is raised.
    int lenShape = ptShape.Length,  lenWidth = ptWidth.Length, lenWeight = ptWeight.Length; // all guaranteed null, being protected.
    int lenPtTintArray = ptTintArray.Length;
    int lenTexts = texts._Length();
    double x, y, w, wide=0, oldWide = -1,  PIPI = Math.PI * 2.0;
    int thisIndex, noneYet = Int32.MaxValue, lastIndex = noneYet;
    gr.NewPath();
    gr.SetDash(new double[0], 0);
    Pango.Layout layout = null;
    double pangoIncX = 0, pangoIncY = 0; // offsets used to centre texts, based on trial text "O". See under Case '$' below.
    for (int pt = 0; pt < len; pt++)
    { thisIndex = ptWeight[pt % lenWeight];
      if (thisIndex < 0) continue; // No point, if so.
      if (thisIndex != lastIndex)
      { if (lastIndex != noneYet) gr.Stroke();
        Tint tinto = ptTintArray[thisIndex % lenPtTintArray];
        gr.SetSourceRGB(tinto.r, tinto.g, tinto.b);
        oldWide = wide;
        wide = ptWidth[thisIndex % lenWidth]; // code below must not alter 'wide', as its value is to be preserved between loopings.
      }
      x = XPx[pt];  y = YPx[pt];
      switch (ptShape[thisIndex % lenShape])
      { // Note that gr.Stroke() is done once for all at the end of the FOR loop through points.
        case '.': // SOLID CIRCLES
        { gr.NewSubPath(); // Otherwise a line is drawn between the last thingo on the path and the arc.
          gr.Arc(x, y, wide / 2.0, 0, PIPI);
          gr.Fill();
          break;
        }
        case 'o': // EMPTY CIRCLES
        { gr.NewSubPath();
          gr.LineWidth = 1.5;
          gr.Arc(x, y, wide / 2.0, 0, PIPI);
          break;
        }
        case 'O': // EMPTY CIRCLES WITH THICKER LINE
        { gr.NewSubPath();
          gr.LineWidth = 3;
          gr.Arc(x, y, wide / 2.0, 0, PIPI);
          break;
        }
        case 'x': // 'X' WITH THINNER LINES
        { gr.NewSubPath();
          gr.LineWidth = 1.5;  w = wide / 2;
          gr.MoveTo(x-w, y-w);  gr.LineTo(x+w, y+w);
          gr.MoveTo(x-w, y+w);  gr.LineTo(x+w, y-w);
          break;
        }
        case 'X': // 'X' WITH THICKER LINES
        { gr.NewSubPath();
          gr.LineWidth = 3.0;  w = wide / 2;
          gr.MoveTo(x-w, y-w);  gr.LineTo(x+w, y+w);
          gr.MoveTo(x-w, y+w);  gr.LineTo(x+w, y-w);
          break;
        }
        case '+': // '+' WITH THINNER LINES
        { gr.NewSubPath();
          gr.LineWidth = 1.5;  w = wide / 2;
          gr.MoveTo(x-w, y);  gr.LineTo(x+w, y);
          gr.MoveTo(x, y-w);  gr.LineTo(x, y+w);
          break;
        }
        case '#': // '+' WITH THICKER LINES
        { gr.NewSubPath();
          gr.LineWidth = 3.0;  w = wide / 2;
          gr.MoveTo(x-w, y);  gr.LineTo(x+w, y);
          gr.MoveTo(x, y-w);  gr.LineTo(x, y+w);
          break;
        }
        case '[': // SOLID SQUARE
        { gr.NewSubPath();
          gr.LineWidth = 1.5;  w = wide / 2;
          gr.Rectangle(x-w, y-w, wide, wide);
          gr.Fill();
          break;
        }
        case ']': // EMPTY SQUARE
        { gr.NewSubPath();
          gr.LineWidth = 1.5;  w = wide / 2;
          gr.Rectangle(x-w, y-w, wide, wide);
          break;
        }
        case '$': // TEXT        // We use Pango rather than Cairo's inbuilt font handler
        { if (lenTexts > 0)
          { if (layout == null || oldWide != wide)
            { layout = Pango.CairoHelper.CreateLayout(gr);
              layout.FontDescription = Pango.FontDescription.FromString(FontName + " " + wide.ToString());
              layout.SetText("O");
              Pango.Rectangle goodone, dummyone;
              layout.GetExtents(out dummyone, out goodone);
              pangoIncX = JTV.ToPointsDouble(goodone.Width / 2.0); // No offset --> letter's top left corner goes at (x,y).
              pangoIncY = JTV.ToPointsDouble(goodone.Height / 2.0);
            }
            int l, m = pt % lenTexts;
            if (texts[m] != null)
            { l = texts[m].Length;
              if (l > 0)
              { gr.NewSubPath();
                gr.MoveTo(x - pangoIncX, y - pangoIncY );
                layout.SetText(texts[m]);
                Pango.CairoHelper.ShowLayout(gr, layout);
              }
            }
          }
          break;
        }
        default: break; // But don't break out of the loop; e.g. the user may want every 2nd. point not to plot, so has set ptShape to be ['.', ' '];
                        //  there is no "case" for the space char. above, so it would fall through to here.
      }
      gr.Stroke();
      lastIndex = thisIndex;
    } // End of loop through points
    if (layout != null) layout.Dispose();
  }

/// <summary>
/// <para>A line from point i to point i+1 is drawnif LnWeight[i] is not negative. If field CloseLoop is true, the last point will be joined
///  to the fist. Where part of a joint is off the graph, the joint will be displayed to the extent that it lies within the graph's perimeter.</para>
/// <para>PointWt (a doctored temporary copy of field PtWeight) is used to discern when points are off the graph.</para>
/// <para>Pixel values are referred to the rectangle of the Board's drawing area, not to the graph perimeter. It is up to calling code
///  to do the conversion and in the process to set PointWt[] values to -1 for points which are off the board.</para>
/// <para>Calling code MUST ensure that the two array args. have the same nonzero length, or else no plotting happens.</para>
/// <para> LINE SHAPES: Valid Shape values are:  '_' (continuous line), '-' (dashed line), '.' (dotted line), '!' (dash-dot repeatedly),
///  ':' (dash-dot-dot), '?' (use field Dashing directly). Anything else results in no line plot.</para>
/// <para>Fill: only accessed if CloseLoop is present; if TRUE, then FillCcr is accessed and used.</para>
/// <para>TransXTransYRotn:  If present, translation occurs of X values by [0] and of Y values by [1]; rotation by [2] radians; all default to 0.
/// Translations must be in pixel values and rotation in radians.</para>
/// </summary>c
  public void Join(Cairo.Context gr, double[] XPx,  double[] YPx, bool Fill, Tint FillTint)
  { int len = XPx._Length(),  lenWeight = lnWeight._Length();
    if (len < 1 || YPx._Length() != len || lenWeight < 1) return; // No lines will be drawn, but no error is raised.
    int lenShape = lnShape._Length(),  lenWidth = lnWidth._Length();
    int lenLnTintArray = lnTintArray._Length();
    if (lenShape < 1 || lenWidth < 1 || lenLnTintArray < 1) return; // No lines will be drawn, but no error is raised.
    int thisIndex, noneYet = Int32.MaxValue, lastIndex = noneYet;
    char shape = ' ';
    gr.NewPath();
// *** see if there is any way around the intermediate gr.Stroke() a few lines down, so that the path can be coloured.
// If so, then use Cairo's closeloop. Otherwise if Fill is true and LnWeight is longer than 1, have to draw a preliminary
// path with no line, fill that, and then superimpose the unfilled polygon.
    bool separateFillPathNeeded = false; // If there is any change of line features (i.e. if lnWeight has more than one unique value),
                                         //  separate path needed for fills. Only accessed if arg. Fill is true.
    for (int pt = 0; pt < len; pt++)
    { thisIndex = lnWeight[pt % lenWeight];
      if (thisIndex < 0) continue; // No line, if so. Also, leave lastIndex as it is, representing the last valid lnWeight setting.
      if (thisIndex != lastIndex)
      { if (lastIndex != noneYet)  { gr.Stroke(); separateFillPathNeeded= true; }
        gr.MoveTo(XPx[pt], YPx[pt]);
        Tint tinto = lnTintArray[thisIndex % lenLnTintArray];
        gr.SetSourceRGB(tinto.r, tinto.g, tinto.b);
        gr.LineWidth = lnWidth[thisIndex % lenWidth];
        shape = lnShape[thisIndex % lenShape]; // In the event of error, will be reset to ' '.
        switch (shape)
        { case '_': // CONTINUOUS LINE
          { gr.SetDash(new double[0], 0.0);  break; } // turn dash-setting off.
          case '-': // DASHED LINE
          { gr.SetDash(new double[] {3.0, 2.0}, 0.0); break; }
          case '.': // DOTTED LINE
          { gr.SetDash(new double[] {1.5, 1.5}, 0.0); break; }
          case '!': // DASH-DOT
          { gr.SetDash(new double[] {4.0, 2.0, 1.5, 2.0}, 0.0); break; }
          case ':': // DASH-DOT-DOT
          { gr.SetDash(new double[] {4.0, 2.0, 1.5, 2.0, 1.5, 2.0}, 0.0); break; }
          case '?': // DASH-DOT-DOT
          { if (dashPattern == null) shape = ' ';
            else gr.SetDash(dashPattern, 0.0);
            break;
          }
          default: { shape = ' '; break; }
        }
        lastIndex = thisIndex;
      }
      if (shape != ' ') // then no error, so we can go ahead and draw the line:
//######## For shape ' ' and for thisIndex negative, finish the existing path and reset lastIndex to -1, triggering new path.
      { if (pt < len-1) gr.LineTo(XPx[pt+1], YPx[pt+1]);
        else if (closeLoop) gr.LineTo(XPx[0], YPx[0]);
      }
    } // end of FOR loop through points
    if (lastIndex != noneYet)
    { // re FILLING:
      if (closeLoop && Fill)
      { if (separateFillPathNeeded) // then gr.Stroke() was invoked above (at change of line details), so we have no complete path to fill:
        { gr.Save();
          gr.NewPath(); // redraw the path
          gr.LineWidth = 0;
          gr.MoveTo(XPx[0], YPx[0]);
          for (int pt=1; pt < len; pt++) gr.LineTo(XPx[pt], YPx[pt]);
          gr.LineTo(XPx[0], YPx[0]);
          gr.SetSourceRGB(FillTint.r, FillTint.g, FillTint.b);
          gr.Fill();
          gr.Restore();
          gr.Stroke();
        }
        else // fill, but gr.Stroke() was never invoked in the big loop higher up, so there is a complete path that can be filled:
        { gr.StrokePreserve();
          if (closeLoop && Fill && !separateFillPathNeeded) 
          { gr.SetSourceRGB(FillTint.r, FillTint.g, FillTint.b);
            gr.Fill(); 
          }
        }
      }
      else gr.Stroke();
    }
  }

  /// <summary>
  /// <para>VOID; sets the 'out' args. to the maximum and minimum value in the set of coordinates belonging to the axis 'Axis'.
  ///  'Axis' must be one of: 'X', 'Y', 'Z' (case insensitive), or else a mighty crash occurs.</para>
  /// <para>If the coordinates array is null or empty, returns with crossed values: Maximum &lt; Minimum (being minimum and maximum possible values
  ///   respectively for type double).</para>
  /// </summary>
 public virtual void ExtremeValues(char Axis, out double Minimum, out double Maximum)
  { Minimum = double.MaxValue;   Maximum = double.MinValue;
    double[] Arr;
    Axis = char.ToUpper(Axis);
    if (Axis == 'X') Arr = XX;  else if (Axis == 'Y') Arr = YY;  else if (is3D && Axis == 'Z') Arr = ZZ;
    else throw new Exception("ExtremeValues(.) - illegal value '" + Axis + "' for 'Axis'");
    int len = Arr._Length();
    for (int i=0; i < len; i++)
    { if (Arr[i] < Minimum) Minimum = Arr[i];
      if (Arr[i] > Maximum) Maximum = Arr[i];
    }
  }

  /// <summary>
  /// Given 3D points, return their pixel positions for the given graph. The 3 input arrays must have equal
  ///   length, that length being at least 1. ERROR returns FALSE and also sets the 'out' arrays to NULL.
  /// </summary>
  public static bool Projections(Graph G, Plot P)
  { double[] Xx = P.XX,  Yy = P.YY,  Zz = P.ZZ; // Local pointers, to save time
    int noPoints = Xx._Length();  if (noPoints < 1 || Yy._Length() != noPoints || Zz._Length() != noPoints) return false;
    P.XPx = new double[noPoints];   P.YPx = new double[noPoints];
    double[] Xpix = P.XPx,   Ypix = P.YPx; // Local pointers, to save time
    double axisO_X = G.UserO.X,  axisO_Y = G.UserO.Y,
           axisX_X = G.UserX.X,  axisX_Y = G.UserX.Y,
           axisY_X = G.UserY.X,  axisY_Y = G.UserY.Y,
                                 axisZ_Y = G.UserZ.Y;
    double coeffX = 1.0 / (G.HighX - G.LowX),
           coeffY = 1.0 / (G.HighY - G.LowY),
           coeffZ = 1.0 / (G.HighZ - G.LowZ);
    double ax, ay, az,   lowX = G.LowX, lowY = G.LowY, lowZ = G.LowZ;
    for (int i=0; i < noPoints; i++)
    { ax = (Xx[i] - lowX) * coeffX;   ay = (Yy[i] - lowY) * coeffY;   az = (Zz[i] - lowZ) * coeffZ; //##### new
      Xpix[i] = axisO_X * (1 - ax - ay)      + axisX_X * ax + axisY_X * ay;
      Ypix[i] = axisO_Y * (1 - ax - ay - az) + axisX_Y * ax + axisY_Y * ay + axisZ_Y * az;
    }
    return true;
  }


  } // End of class Plot
//==============================================

  public class Mesh : Plot                                        //mesh
  { // Curves built from rows of the coordinate matrices below are called 'forward' curves, and those built from columns, 'transverse' curves.
    // Note the policy of not bothering about hiding coordinate matrices from user code. The data is likely to be horrendously large at times,
    //   and the less copying that is done the better. But this puts it on the user to be sure NOT to allow pointers to data to persist.
    // PLOT FIELDS are heavily used. For forward curves, the nth. point of every curve has the same features, and lines and texts likewise.
    //   The only extra fields deal with the transverse curves, which likewise behave in unison.

   // COORDINATES:  *** NB: NOT COPIED -- user-code data is pointer-referenced. ***
    public double[][] MX, MY, MZ; // Point coordinates in user space. Defined as ragged matrices so that whole rows can be extracted;
          // but row lengths must all be the same, and also the no. rows and cols. in MX must be the same as in MY. The property tests for
          // these conditions will be done in the Draw method.
   // WEIGHTS: // During drawing, these overwrite the corresponding Plot fields first with 'trv..' and then with 'fd...'.
    protected int[] fdPtWeight;
      public int[] FdPtWeight
      { get { return (int[]) fdPtWeight.Clone(); }
        set { int len = value._Length();  if (len > 0) { fdPtWeight = (int[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      }
      // There is no 'trvPtWeight'; instead Plot.ptWeight is overwritten by a null version that stops points being plotted.
    protected int[] fdLnWeight;
      public int[] FdLnWeight
      { get { return (int[]) fdLnWeight.Clone(); }
        set { int len = value._Length();  if (len > 0) { fdLnWeight = (int[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      }
    protected int[] trvLnWeight;
      public int[] TrvLnWeight
      { get { return (int[]) trvLnWeight.Clone(); }
        set { int len = value._Length();  if (len > 0) { trvLnWeight = (int[]) value.Clone();  PlotRevisionTime = DateTime.Now.Ticks; } }
      }
   // LOOPING -- there is only one bool for all forward curves ( = the Plot field) and one for all transverse curves (below).
   // These values will overwrite the Plot field .closeloop during drawing.
    protected bool fdCloseLoop; // If TRUE, the last point in XPx will be connected to the first point.
      public bool FdCloseLoop { get { return fdCloseLoop; } set { fdCloseLoop = value;  PlotRevisionTime = DateTime.Now.Ticks; } }
    protected bool trvCloseLoop; // If TRUE, the last point of every transverse curve will be connected to its first point.
      public bool TrvCloseLoop { get { return trvCloseLoop; } set { trvCloseLoop = value;  PlotRevisionTime = DateTime.Now.Ticks; } }

// CONSTRUCTORS
    // The  base constructor has set the artID value.
    protected Mesh() : base(0)
    { MX = null;  MY = null;
      fdPtWeight = new int[] { 0, 0, 0, 0, 0 }; // 5 values to cut down no. of rotational resets.
      fdLnWeight = new int[] { 0, 0, 0, 0, 0 }; // 5 values to cut down no. of rotational resets.
      trvLnWeight = new int[] { 0, 0, 0, 0, 0 }; // 5 values to cut down no. of rotational resets.
      fdCloseLoop = false;    trvCloseLoop = false;
    }
    // This constructor sets MX and MY and possibly MZ (all by POINTER REFERENCE!). If MZ is NULL, this is taken as a 2D
    // plot. Apart from this, the argument matrices must not be null or empty, or crasho.
    // 'is3D' remains FALSE (as set by the Plot base constructor)
    // MXPx and MYPx are not set here; they are set by the first call to the drawing method.
    public Mesh(double[][] XCoords, double[][] YCoords, double[][] ZCoords) : this()
    { is3D = (ZCoords != null);
      if (XCoords._NorE() || YCoords._NorE() || (is3D && YCoords.Length == 0) )
      { throw new Exception("The Mesh constructor cannot be given null or empty arrays"); }
      MX = XCoords;    MY = YCoords; // *** That dangerous pointer assignment... ***
      if (is3D) MZ = ZCoords;
    }

    // This constructor sets the coordinates and the basic point and line arrays which are inherited from class Plot. It does
    // NOT set: ptWeight, lnWeight, xLnWeight, closeLoop, xCloseLoop, texts.
    // For 2D graphs, set ZCoords to NULL.
    public Mesh(double[][] XCoords, double[][] YCoords, double[][] ZCoords, char[] PtShape_, double[] PtWidth_, Gdk.Color[] PtClr_,
                                                 char[] LnShape_, double[] LnWidth_, Gdk.Color[] LnClr_ ) : this(XCoords, YCoords, ZCoords)
    { if (!PtShape_._NorE()) ptShape = (char[])    PtShape_.Clone();
      if (!PtWidth_._NorE()) ptWidth = (double[])  PtWidth_.Clone();
      if (!PtClr_._NorE())   ptTintArray = Tint.TintArray(PtClr_);
      if (!LnShape_._NorE()) lnShape = (char[])    LnShape_.Clone();
      if (!LnWidth_._NorE()) lnWidth = (double[])  LnWidth_.Clone();
      if (!LnClr_._NorE())   lnTintArray   = Tint.TintArray(LnClr_);
    }

/// <summary>
/// This method is only ever called from the parent class Plot's static method 'Draw(.)'.
/// </summary>
    public void DrawMesh(Graph ThisGraph, Cairo.Context gr, double width, double height)
    { int noFdCurves, noTrvCurves;
      int[] xi = this.MX._JaggedLengths();    noFdCurves = xi.Length;  noTrvCurves = xi[0];
      if (noFdCurves < 2 || noTrvCurves < 2) return;
      for (int i=1; i < noFdCurves; i++) { if (xi[i] != noTrvCurves) return; } // all rows must have the same length.
      int[] yi = this.MY._JaggedLengths();
      if (yi.Length != noFdCurves) return;
      for (int i=0; i < noFdCurves; i++) { if (yi[i] != xi[i]) return; }
      if (this.is3D)
      { int[] zi = this.MZ._JaggedLengths();
        if (zi.Length != noFdCurves) return;
        for (int i=0; i < noFdCurves; i++) { if (zi[i] != xi[i]) return; }
      }
    // TRANSVERSE CURVES: Drawn first so that points will be plotted on top of all lines.
      // Similarly preserve the point weights, which we'll temporarily overwrite:
      ptWeight = new int[] {-1, -1, -1, -1, -1}; // We don't want to replot the points.
      lnWeight = TrvLnWeight; // RHS is a property. The point of using the property is to have the field copied, not just referenced.
      closeLoop = TrvCloseLoop;
      XX = new double[noFdCurves];    YY = new double[noFdCurves]; // no. transverse points = the no. forward curves.
      if (is3D) ZZ = new double[noFdCurves];
      for (int curve = 0; curve < noTrvCurves; curve++)
      { for (int j=0; j < noFdCurves; j++)
        { XX[j] = MX[j][curve];   YY[j] = MY[j][curve];  if (is3D) ZZ[j] = MZ[j][curve]; }
        DrawPlot(ThisGraph, gr, width, height);
      }
    // FORWARD CURVES:
      ptWeight = FdPtWeight;
      lnWeight = FdLnWeight;
      closeLoop = FdCloseLoop;
      XX = new double[noTrvCurves];    YY = new double[noTrvCurves]; // no. transverse points = the no. forward curves.
      if (is3D) ZZ = new double[noTrvCurves];
      for (int curve = 0; curve < noFdCurves; curve++)
      {// Set up parent class instance fields:
        XX = MX[curve];  YY = MY[curve];   if (is3D) ZZ = MZ[curve];
        DrawPlot(ThisGraph, gr, width, height);
      }
    }

  /// <summary>
  /// <para>VOID; sets the 'out' args. to the maximum and minimum value in the set of coordinates belonging to the axis 'Axis'.
  ///  'Axis' must be one of: 'X', 'Y', 'Z' (case insensitive), or else a mighty crash occurs.</para>
  /// <para>If the coordinates array is null or empty, returns with crossed values: Maximum &lt; Minimum (being minimum and maximum possible values
  ///   respectively for type double).</para>
  /// </summary>
 public override void ExtremeValues(char Axis, out double Minimum, out double Maximum)
  { Minimum = double.MaxValue;   Maximum = double.MinValue;
    double[][] Mx = null;
    Axis = char.ToUpper(Axis);
    if (Axis == 'X') Mx = MX;  else if (Axis == 'Y') Mx = MY;  else if (is3D && Axis == 'Z') Mx = MZ;
    else throw new Exception("ExtremeValues(.) - illegal value '" + Axis + "' for 'Axis'");
    int noRows = Mx.Length,  noCols = Mx[0].Length;
    for (int i=0; i < noRows; i++)
    { double[] row = Mx[i];
      for (int j=0; j < noCols; j++)
      { if (row[j] < Minimum) Minimum = row[j];
        if (row[j] > Maximum) Maximum = row[j];
      }
    }
  }



  } // End of class Mesh
//---------------------------------------------------------------
// The spiel for the public constructor goes here, as this is where remarks are sought from hovering over the constructor in code elsewhere.
  /// <summary>
  /// <para>Creates the basic shape, ready for movement by dynamic settings of MagnifyX/Y, PivotX/Y and Rotatn. The shape is constructed
  /// with Pivot of (0,0), which should be born in mind if magnifying or rotating; i.e. choose coords. so that the structure is not lopsided.</para>
  /// <para>Returns a dud (ArtID = 0) if ShapeType is unrecognized.</para>
  /// <para>X/YCoords: (a) they must be of equal length, and (b) that length must be at least the minimum declared in field minCoords[] for this shape.
  /// Failure returns a dud shape (ArtID = 0). If the arrays have more points than are needed for some fixed-size shape (e.g. rectangle),
  /// excess coordinates are simply ignored and not stored.</para>
  /// </summary>
  public class Shape : Plot                           //shape
  {
  // STATIC FIELDS:
    private static Tint DefFillTint = new Tint(1.0 ,1.0, 1.0); // white
    public static Gdk.Color DefFillClr { get { return Tint.GDKClr(DefFillTint); }  set { DefFillTint = new Tint(value); } }

    protected static string ShapeTypes = "AEPR"; // Arc, Ellipse, Polygon, Rectangle. The arc is an elliptical arc.

  // INSTANCE FIELDS:  **** IF ADDING / ALTERING, be sure to adjust method CopyShape(.).
    /// <summary>
    /// Note that PivotX/Y are ref. points for rotation and magnification; unless the users sets it so, it is not the natural centre of a shape.
    /// </summary>
    public double PivotX, PivotY; // Each (x,y) of XX[], YY[] will be translated to (x + PivotX, y + PivotY).
    public double Rotatn; // The structure of XX[], YY[] will be rotated around (PivotX, PivotY), using the conventional positive angle direction.
      /// <summary>
      /// Gets or sets rotation angle in degrees. No rationalization of angles beyond +/- PI; e.g. 1800 deg. <--> 10.PI.
      /// </summary>
      public double RotatnDeg
      { get { return 180.0 * Rotatn / Math.PI; }   set { Rotatn = value * Math.PI / 180.0; }  }
    public double MagnifyX, MagnifyY; // Before rotation and translation, (x,y) --> (MagnifyX * x, MagnifyY * y). For this not to cause
                                      // translation, (PivotX, PivotY) should be visually the centre of the structure.
    public bool FillShape;
    protected Tint fillTint;
      public Gdk.Color FillClr { get { return Tint.GDKClr(fillTint); }  set { fillTint = new Tint(value); } }

    public double[] CornersX, CornersY; // These are safe as 'public' because they are generated afresh in DrawPolygon(.) and never used
                                        //  after that method has run; they can be accessed from outside, and changing them there will do harm.

//----------------------------------------------------------
//   CONSTRUCTORS
//----------------------------------------------------------
    // The  base constructor sets the artID value and sets line parameters.
    protected Shape() : base()
    {
      PivotX = PivotY = 0.0;  Rotatn = 0.0;  MagnifyX = MagnifyY = 1.0;
      FillShape = false; //       closeLoop was set to false in the Plot base constructor.
      fillTint = Tint.CopyOf(DefFillTint); // Even though FillShape is false, we put in a default colour, in case user sets FillShape to TRUE and forgets a colour.
    }

    // Creates the basic shape, ready for movement by dynamic settings of MagnifyX/Y, PivotX/Y and Rotatn. The shape is constructed
    // with Pivot of (0,0), which should be born in mind if magnifying or rotating; i.e. choose coords. so that the structure is not lopsided.
    // Returns a dud (ArtID = 0) if ShapeType is unrecognized.
    // In the present implementation, all shapes are converted to polygons within this constructor, and when drawn are drawn as polygons only.
    // ShapeType is one of A(rc), E(llipse), P(olygon), R(ectangle).
    // COORD. FORMATS, all applying to the unrotated, unmagnified, untranslated shape: (Higher values of coord. arrays simply ignored.)
    //   Ellipse: X[0] = X axis radius, Y[0] = Y axis radius;X[1], if >= 1, sets straight segments per quarter-circumf. (default 10); Y[1] a dummy.
    //   Rectangle: X[0], X[1] = min. and max. X of corners (any order); Y[0], Y[1] analogous.
    //   Polygon: (X[i], Y[i]) is the ith. apex (base 0). Min lengths 3.
    //   Arc: Elliptical radii X[0], Y[0]; start/end angles X[1], Y[1] (any order); X[2] (>= 4) = no segments for whole arc (default 4);
    //        Y[2]: 0 = curve only, nothing to fill; 1 = draw chord, and fill; 2 = draw sector (i.e. include the origin in the curve), and fill.

    public Shape(char ShapeType, double[] XCoords, double[] YCoords, char[] LnShape_, double[] LnWidth_,
                                                                     Gdk.Color[] LnClr_, Gdk.Color FillClr_  ) : this()
    { if (artID == 0) return;
      fillTint = new Tint(FillClr_);

      // Array args:
      if (!LnShape_._NorE()) lnShape = (char[])    LnShape_.Clone(); // Note that default line characteristics were set in 'base()' (Plot constructor).
      if (!LnWidth_._NorE()) lnWidth = (double[])  LnWidth_.Clone();
      if (!LnClr_._NorE())   lnTintArray = Tint.TintArray(LnClr_);

      FillShape = true; // If the user does not want the shape filled, he would have to precede drawing by setting this to be false.
      closeLoop = true; // The default, which is reset to false only by ShapeType 'A':
      int coordsLen = XCoords._Length();   if (YCoords._Length() != coordsLen) { artID = 0;  return; }

      if (ShapeType == 'A')
      { if (coordsLen < 3) { artID = 0; return; }
        double horizR = XCoords[0], vertR = YCoords[0];
        double startAngle = JM.AngleToRange(XCoords[1]);
        double endAngle   = JM.AngleToRange(YCoords[1]);
        int noPoints = 1 + (int) XCoords[2];  if (noPoints < 5) noPoints = 5; // 5 needed for an unclosed arc of nearly 2PI to look like trapezium.
        int howToCompleteCurve = (int) YCoords[2]; // should be 0, 1, 2; all others default to action of 0.
        double incAngle = (endAngle - startAngle) / (double) (noPoints-1);
        double[] Xoo, Yoo;
        if (howToCompleteCurve == 2) { Xoo = new double[noPoints+1];  Yoo = new double[noPoints+1]; } // sector, so origin included
        else                         { Xoo = new double[noPoints];    Yoo = new double[noPoints]; }
        closeLoop = (howToCompleteCurve == 1 || howToCompleteCurve == 2);
        double cos, sin, angle, radius, HV = horizR * vertR, H2 = horizR * horizR,  V2 = vertR * vertR;
        for (int i=0; i < noPoints; i++)
        { angle = startAngle + i * incAngle;   cos = Math.Cos(angle);   sin = Math.Sin(angle);
          radius = HV / (Math.Sqrt(V2 * cos * cos + H2 * sin * sin) );
          Xoo[i] = radius * cos;   Yoo[i] = radius * sin;
        }
        if (howToCompleteCurve == 2) { Xoo[noPoints] = Yoo[noPoints] = 0.0; }
        XX = Xoo;  YY = Yoo;
      }
      else if (ShapeType == 'P')
      { if (coordsLen < 3) { artID = 0; return; }
        XX = new double[coordsLen];  YY = new double[coordsLen];
        for (int i=0; i < coordsLen; i++) { XX[i] = XCoords[i];  YY[i] = YCoords[i]; }
      }
      else if (ShapeType == 'E')
      { if (coordsLen < 2) { artID = 0; return; }
        // Convert coordinates to polygon format:
        int noSteps = 10; // No. axis segments for a quarter circle.
        if (XCoords[1] >= 1.0) noSteps = (int) XCoords[1];
        double[] Eks = new double[4 * noSteps],  Why = new double[4 * noSteps];
        double x=0, y=0, ang = 0;
        double horizR = XCoords[0], vertR = YCoords[0];
        double H = 1/(horizR * horizR);
        // Points on the X axis:
        Eks[0] = horizR;  Why[0] = 0.0;               Eks[2*noSteps] = -horizR;  Why[2*noSteps] = 0.0;
        // Points on the Y axis:
        Eks[noSteps] = 0.0;  Why[noSteps] = vertR;    Eks[3*noSteps] = 0.0;  Why[3*noSteps] = -vertR;
        // All the other points:
        for (int i = 1; i < noSteps; i++)
        { ang = i * Math.PI / (2.0 * noSteps);
          x = horizR * Math.Cos(ang); // steps of x that would give equal arc lengths; we'll suppose they are good enough for an ellipse also.
          y = vertR * Math.Sqrt(1.0 - x * x * H);
          Eks[i] = Eks[4*noSteps - i] = x;
          Eks[2*noSteps - i] = Eks[2*noSteps + i] = -x;
          Why[i] = Why[2*noSteps - i] = y;
          Why[2*noSteps + i] = Why[4*noSteps - i] = -y;
        }
        XX = Eks;  YY = Why;
      }
      else if (ShapeType == 'R')
      { if (coordsLen < 2) { artID = 0; return; }
       // Convert coordinates to polygon format, hence requiring that coordinate arrays have length 4, not 2.
        double z, xlo = XCoords[0], xhi = XCoords[1], ylo = YCoords[0], yhi = YCoords[1];
        if (xlo > xhi) { z = xlo;  xlo = xhi;  xhi = z; }  else if (xlo == xhi) return;
        if (ylo > yhi) { z = ylo;  ylo = yhi;  yhi = z; }  else if (ylo == yhi) return;
        XX = new double[] {xhi, xlo, xlo, xhi};  YY = new double[] {yhi, yhi, ylo, ylo};
      }
      else { artID = 0;  return; } // unrecognized type.
    }
    /// <summary>
    /// <para>Creates a copy of OldShape, the only modification being any supplied parameters: [0] = PivotX, [1] = PivotY, [2] = MagnifyX,
    ///  [3] = MagnifyY, [4] = Rotatn. Missing parameters default to those of OldShape.</para>
    /// <para>A dud OldShape (i.e. one with ArtID = 0) returns NULL.</para>
    /// </summary>
    public static Shape CopyOf(Shape OldShape, params double[] pivXYmagXYrotn)
    { if (OldShape.artID == 0) return null;
      Shape result = new Shape();
      // Set this method's argument fields:
      int arglen = pivXYmagXYrotn.Length;
      if (arglen > 0) result.PivotX   = pivXYmagXYrotn[0];  else result.PivotX   = OldShape.PivotX;
      if (arglen > 1) result.PivotY   = pivXYmagXYrotn[1];  else result.PivotY   = OldShape.PivotY;
      if (arglen > 2) result.MagnifyX = pivXYmagXYrotn[2];  else result.MagnifyX = OldShape.MagnifyX;
      if (arglen > 3) result.MagnifyY = pivXYmagXYrotn[3];  else result.MagnifyY = OldShape.MagnifyY;
      if (arglen > 4) result.Rotatn   = pivXYmagXYrotn[4];  else result.Rotatn   = OldShape.Rotatn;
      // Copy all other fields: (Extension '_Clone()' is used sometimes instead of 'Clone()' so that null OldShape arrays do not crash the method.)
      result.closeLoop   = OldShape.closeLoop;
      result.fillTint = Tint.CopyOf(OldShape.fillTint);
      result.FillShape   = OldShape.FillShape;
      result.lnTintArray = Tint.CopyOfArray(OldShape.lnTintArray);
      result.lnShape     = (char[]) OldShape.lnShape.Clone();
      result.lnWeight    = (int[]) OldShape.lnWeight.Clone();
      result.lnWidth     = (double[]) OldShape.lnWidth.Clone();
      result.XX          = (double[]) OldShape.XX.Clone();
      result.YY          = (double[]) OldShape.YY.Clone();
      return result;
    }

/// <summary>
/// This method is only ever called from the parent class Plot's static method 'Draw(.)'.
/// </summary>
    public void DrawShape(Graph ThisGraph, Cairo.Context gr, double width, double height)
    { if (ArtID == 0) return;
      DrawPolygon(ThisGraph, gr, width, height);
    }

    protected void DrawArc(Graph ThisGraph, Cairo.Context gr, double width, double height)
    {

    }

    protected void DrawEllipticArc(Graph ThisGraph, Cairo.Context gr, double width, double height)
    {

    }

    protected void DrawCircle(Graph ThisGraph, Cairo.Context gr, double width, double height)
    {

    }

    protected void DrawEllipse(Graph ThisGraph, Cairo.Context gr, double width, double height)
    {

    }

    protected void DrawPolygon(Graph ThisGraph, Cairo.Context gr, double width, double height)
    { int len = XX._Length();  if (len < 3 || YY.Length != len) return;
     // Set local variables to graph base variables, as they will be referenced repeatedly in loops:
      double bxLeft = ThisGraph.BoxLeft,   bxBottom = ThisGraph.BoxBottom;
      double bxWidth = ThisGraph.BoxWidth, bxHeight = ThisGraph.BoxHeight;
      double LoX = ThisGraph.LowX,  HiX = ThisGraph.HighX,  LoY = ThisGraph.LowY,  HiY = ThisGraph.HighY;
      double SpanX = HiX - LoX,   SpanY = HiY - LoY;
     // We shall keep the original values of XX, YY, and apply magnification, translation and rotation only to copies of them.
      CornersX = new double[len];  CornersY = new double[len]; // public class arrays; values are therefore accessible directly
               // from outside. This is safe because values are never reused in any method of this class.
     // Magnify:
      for (int i=0; i < len; i++) { CornersX[i] = XX[i] * MagnifyX;  CornersY[i] = YY[i] * MagnifyY; }
     // Rotate and Translate:
      double x, y, cosA = Math.Cos(Rotatn),  sinA = Math.Sin(Rotatn);
      for (int i=0; i < len; i++)
      { x = CornersX[i];  y = CornersY[i];
        CornersX[i] = cosA*x - sinA*y + PivotX;  CornersY[i] = sinA*x + cosA*y + PivotY;
      }
     // Develop the points:
      XPx = new double[len];     YPx = new double[len];
      double toobig = bxWidth * bxHeight + 5; // If we tie XPx and YPx values to +/- this, then in the worst case, a line from
        // a point (x1,y1) on the graph to a point e.g. (x2, truncated y2) is only 1/5 of a pixel from the line to the untruncated point.
        // By experimentation this was found necessary, as Cairo inverts the sign of an oversized coord. if it is very much bigger than that.
      double toosmall = -toobig;
      double spannerX = bxWidth / SpanX,  spannerY = bxHeight / SpanY;
      for (int i=0; i < len; i++)
      { x = bxLeft   + (CornersX[i] - LoX) * spannerX;
        if (x < toosmall) x = toosmall;  else if (x > toobig) x = toobig;
        y = bxBottom - (CornersY[i] - LoY) * spannerY;
        if (y < toosmall) y = toosmall;  else if (y > toobig) y = toobig;
        XPx[i] = x;   YPx[i] = y;
      }
      Join(gr, XPx, YPx, FillShape, fillTint);
    }

  } // End of class Shape

//=================================================================
} // End of namespace JLib

