﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/******************************************************************************
 * Copyright (c) 2013-2014, Justin Bengtson
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 * 
 * 1. Redistributions of source code must retain the above copyright notice,
 * this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 * this list of conditions and the following disclaimer in the documentation
 * and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/

namespace RegexKSP {
	public class NodeManager {
		public NodeState curNodeState;
		public NodeState curState;
		public ManeuverNode node = null;
		public ManeuverNode nextNode = null;
		public bool changed = false;
		public bool encounter = false;
		public bool resizeMainWindow = false;
		public bool resizeClockWindow = false;

		public bool progradeParsed = true;
		public bool radialParsed = true;
		public bool normalParsed = true;
		public bool timeParsed = true;
		public string progradeText = "";
		public string radialText = "";
		public string normalText = "";
		public string timeText = "";

		public NodeManager() {
			curState = new NodeState();
		}

		public NodeManager(ManeuverNode n) {
			curState = new NodeState(n);
			curNodeState = new NodeState();
			node = n;
			updateCurrentNodeState();

			if (NodeTools.findNextEncounter(n) != null) {
				encounter = true;
			}
		}

		public NodeManager nextState() {
			if (nextNode != null) {
				return new NodeManager(nextNode);
			}
			if (NodeTools.findNextEncounter(node) != null) {
				encounter = true;
			}
			return this;
		}

		public void addPrograde(double d) {
			curState.deltaV.z += d;
			progradeText = curState.deltaV.z.ToString();
			changed = true;
		}

		public void setPrograde(String s) {
			double d;
			progradeText = s;
			if (s.EndsWith(".")) {
				progradeParsed = false;
				return;
			}
			progradeParsed = double.TryParse(progradeText, out d);
			if (progradeParsed) {
				if (d != curState.deltaV.z) {
					progradeText = d.ToString();
					curState.deltaV.z = d;
					changed = true;
				}
			}
		}

		public void addNormal(double d) {
			curState.deltaV.y += d;
			normalText = curState.deltaV.y.ToString();
			changed = true;
		}

		public void setNormal(String s) {
			if (normalText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			normalText = s;
			if (s.EndsWith(".")) {
				normalParsed = false;
				return;
			}
			normalParsed = double.TryParse(normalText, out d);
			if (normalParsed) {
				if (d != curState.deltaV.y) {
					normalText = d.ToString();
					curState.deltaV.y = d;
					changed = true;
				}
			}
		}

		public void addRadial(double d) {
			curState.deltaV.x += d;
			radialText = curState.deltaV.x.ToString();
			changed = true;
		}

		public void setRadial(String s) {
			if (radialText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			radialText = s;
			if (s.EndsWith(".")) {
				radialParsed = false;
				return;
			}
			radialParsed = double.TryParse(radialText, out d);
			if (radialParsed) {
				if (d != curState.deltaV.x) {
					radialText = d.ToString();
					curState.deltaV.x = d;
					changed = true;
				}
			}
		}

		public double currentUT() {
			return curState.UT;
		}

		public void addUT(double d) {
			curState.UT += d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		public void setUT(double d) {
			curState.UT = d;
			timeText = curState.UT.ToString();
			changed = true;
		}

		public void setUT(String s) {
			if (timeText.Equals(s, StringComparison.Ordinal)) {
				return;
			}
			double d;
			timeText = s;
			if (s.EndsWith(".")) {
				timeParsed = false;
				return;
			}
			timeParsed = double.TryParse(timeText, out d);
			if (timeParsed) {
				if (d != curState.UT) {
					timeText = d.ToString();
					curState.UT = d;
					changed = true;
				}
			}
		}

		public double currentMagnitude() {
			return curState.deltaV.magnitude;
		}

		public void setPeriapsis() {
			//TODO: Add look-ahead functionality if the current periapsis is non-existant.
			setUT(Planetarium.GetUniversalTime() + node.patch.timeToPe);
		}

		public void setApoapsis() {
			//TODO: Add look-ahead functionality if the current apoapsis is non-existant.
			setUT(Planetarium.GetUniversalTime() + node.patch.timeToAp);
		}

		public bool hasNode() {
			if (node == null) {
				return false;
			}
			return true;
		}

		public void updateNode() {
			// Node manager policy:
			// if the manager has been changed from the last update manager snapshot, take the manager
			// UNLESS
			// if the node has been changed from the last update node snapshot, take the node
			if (curNodeState.compare(node)) {
				// the node hasn't changed, do our own thing
				if (changed) {
					if (node.attachedGizmo != null) {
						node.attachedGizmo.DeltaV = curState.getVector();
						node.attachedGizmo.UT = curState.UT;
					}
					node.OnGizmoUpdated(curState.getVector(), curState.UT);
					updateCurrentNodeState();
					changed = false; // new
				}
			} else {
				// the node has changed, take the node's new information for ourselves.
				updateCurrentNodeState();
				curState.update(node);
			}
		}

		private void updateCurrentNodeState() {
			curNodeState.update(node);
			progradeText = node.DeltaV.z.ToString();
			normalText = node.DeltaV.y.ToString();
			radialText = node.DeltaV.x.ToString();
			timeText = node.UT.ToString();
		}
	}
}