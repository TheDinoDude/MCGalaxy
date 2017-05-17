﻿/*
    Copyright 2015 MCGalaxy
    
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using System.Collections.Generic;
using System.IO;
using MCGalaxy.Network;
using MCGalaxy.Maths;

namespace MCGalaxy.Undo {

    /// <summary> Retrieves and saves undo data in a particular format. </summary>
    /// <remarks> Note most formats only support retrieving undo data. </remarks>
    public abstract partial class UndoFormat {
        
        public static void DoUndo(string target, ref bool found, UndoFormatArgs args) {
            List<string> files = GetUndoFiles(target);
            if (files.Count == 0) return;
            found = true;
            
            foreach (string file in files) {
                using (Stream s = File.OpenRead(file)) {
                    DoUndo(s, GetFormat(file), args);
                    if (args.Stop) break;
                }
            }
        }
        
        public static void DoUndo(Stream s, UndoFormat format, UndoFormatArgs args) {
            Level lvl = args.Player == null ? null : args.Player.level;
            string lastMap = null;
            
            foreach (UndoFormatEntry P in format.GetEntries(s, args)) {
                if (P.LevelName != lastMap) lvl = LevelInfo.FindExact(P.LevelName);
                if (lvl == null || P.Time > args.End) continue;
                
                UndoBlock(args, lvl, P);
            }
        }
        
        
        public static void DoUndoArea(string target, Vec3S32 min, Vec3S32 max,
                                      ref bool found, UndoFormatArgs args) {
            List<string> files = GetUndoFiles(target);
            if (files.Count == 0) return;
            found = true;
            
            foreach (string file in files) {
                using (Stream s = File.OpenRead(file)) {
                    DoUndoArea(s, min, max, GetFormat(file), args);
                    if (args.Stop) break;
                }
            }
        }
        
        public static void DoUndoArea(Stream s, Vec3S32 min, Vec3S32 max,
                                      UndoFormat format, UndoFormatArgs args) {
            Level lvl = args.Player == null ? null : args.Player.level;
            string lastMap = null;
            
            foreach (UndoFormatEntry P in format.GetEntries(s, args)) {
                if (P.LevelName != lastMap) lvl = LevelInfo.FindExact(P.LevelName);
                if (lvl == null || P.Time > args.End) continue;
                
                if (P.X < min.X || P.Y < min.Y || P.Z < min.Z) continue;
                if (P.X > max.X || P.Y > max.Y || P.Z > max.Z) continue;
                UndoBlock(args, lvl, P);
            }
        }
        
        
        public static void DoHighlight(string target, ref bool found, UndoFormatArgs args) {
            List<string> files = GetUndoFiles(target);
            if (files.Count == 0) return;
            found = true;
            
            foreach (string file in files) {
                using (Stream s = File.OpenRead(file)) {
                    DoHighlight(s, GetFormat(file), args);
                    if (args.Stop) break;
                }
            }
        }
        
        public static void DoHighlight(Stream s, UndoFormat format, UndoFormatArgs args) {
            BufferedBlockSender buffer = new BufferedBlockSender(args.Player);
            Level lvl = args.Player.level;
            
            foreach (UndoFormatEntry P in format.GetEntries(s, args)) {
                ExtBlock block = P.Block, newBlock = P.NewBlock;
                byte highlight = (newBlock.BlockID == Block.air
                                  || Block.Convert(block.BlockID) == Block.water || block.BlockID == Block.waterstill
                                  || Block.Convert(block.BlockID) == Block.lava || block.BlockID == Block.lavastill)
                    ? Block.red : Block.green;
                
                buffer.Add(lvl.PosToInt(P.X, P.Y, P.Z), highlight, 0);
            }
            buffer.Send(true);
        }
        
        static void UndoBlock(UndoFormatArgs args, Level lvl, UndoFormatEntry P) {
            byte lvlBlock = lvl.GetTile(P.X, P.Y, P.Z);
            if (lvlBlock == P.NewBlock.BlockID || Block.Convert(lvlBlock) == Block.water
                || Block.Convert(lvlBlock) == Block.lava || lvlBlock == Block.grass) {
                
                if (args.Player != null) {
                    DrawOpBlock block;
                    block.X = P.X; block.Y = P.Y; block.Z = P.Z;
                    block.Block = P.Block;
                    args.Output(block);
                } else {
                    Player.GlobalBlockchange(lvl, P.X, P.Y, P.Z, P.Block); // TODO: rewrite this :/
                    lvl.SetTile(P.X, P.Y, P.Z, P.Block.BlockID);
                    if (P.Block.BlockID != Block.custom_block) return;
                    lvl.SetExtTile(P.X, P.Y, P.Z, P.Block.ExtID);
                }
            }
        }
    }
}