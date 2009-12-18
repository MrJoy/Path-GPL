/*
    Path - pathfinding system for the Unity engine

    Copyright (C) 2008 Emil E. Johansen

    This file is part of Path.

    Path is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 2 of the License, or
    (at your option) any later version.

    Path is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Path.  If not, see <http://www.gnu.org/licenses/>.

    For alternative license options, contact the copyright holder.

    Emil E. Johansen emil@eej.dk
*/


using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PathLibrary
{
	public interface ISearchMonitor
	{
		void OnSearchCompleted( Seeker seeker );
		void OnSearchFailed( Seeker seeker );
		void OnSeekerInvalidated( Seeker seeker );
	}
	
	public class PathData : System.IComparable
	{
		private float gScore, hScore;
		private ConnectionAsset connection;
		private PathData next;

		public PathData( ConnectionAsset connection, float gScore, float hScore )
		{
			this.connection = connection;
			this.next = null;
			this.gScore = gScore;
			this.hScore = hScore;
		}

		public PathData( PathData pathData, ConnectionAsset connection, float gScore, float hScore )
		{
			this.connection = connection;
			this.next = pathData;
			this.gScore = pathData.GScore + gScore;
			this.hScore = pathData.HScore + hScore;
		}

		public float GScore
		{
			get
			{
				return gScore;
			}
		}

		public float HScore
		{
			get
			{
				return hScore;
			}
		}

		public float FScore
		{
			get
			{
				return GScore + HScore;
			}
		}

		public PathData Next
		{
			get
			{
				return next;
			}
		}

		public ConnectionAsset Connection
		{
			get
			{
				return connection;
			}
		}

		public NetworkNodeAsset Destination
		{
			get
			{
				return Connection.To;
			}
		}

		private ArrayList properConnections = null;
		public ArrayList Connections
		{
			get
			{
				if(properConnections == null) {
					ArrayList allConnections, gridNodes;

					allConnections = new ArrayList( Destination.Connections );
					gridNodes = Control.Instance.GetGridNodes( Destination );
						// Get all grid nodes which target our current destination

					if( Destination is GridNodeAsset )
					// If we're in a grid network, also consider the connections of the target
					{
						allConnections.AddRange( ( ( GridNodeAsset )Destination).Target.Connections );
					}

					for( int i = 0; i < gridNodes.Count; i++ )
					// Add grid network connections for all grid nodes targeting our current destination
					{
						allConnections.AddRange( ((GridNodeAsset)gridNodes[i]).Connections );
					}

					if( next == null )
					// If we've only got one node, we needn't do backtracking tests
					{
						properConnections = allConnections;
					} else {
						// TODO: Isn't checking for backtracking meant to be handled in the actual pathfind?
//						properConnections = new ArrayList(allConnections.Count);
						for( int i = allConnections.Count - 1; i >= 0; i-- )
						// Test for backtracking (connection destination equalling connection origin of last connection)
						{
							if( ((ConnectionAsset)allConnections[i]).To == Connection.From )
							// Backtracking. Skip.
							{
								allConnections.RemoveAt(i);
							}
						}
						properConnections = allConnections;
					}
				}
				return properConnections;
			}
		}

		public int CompareTo( object obj )
		{
			PathData other;

			other = obj as PathData;
			if( other != null )
			{
				return FScore.CompareTo( other.FScore );
			}
			else
			{
				throw new System.ArgumentException( "Invalid PathData given" );
			}
		}
	}



	public class Seeker
	{
		private Vector3 from, to;
		private NetworkNodeAsset start, end;
		private ArrayList monitors;
		private List<ConnectionAsset> solution;
//		private PathData solution;
		private float maxFrameTime, radius;
		private object data;
		private string[] requiredTags, excludedTags;
		private bool validateNetworks, seeking;
		private float cacheLifespan;
		
		
		
		public Seeker( Vector3 from, Vector3 to, float maxFrameTime, float radius, object data )
		{
			this.from = from;
			this.to = to;
			this.maxFrameTime = maxFrameTime;
			this.radius = radius;
			this.data = data;
			requiredTags = null;
			excludedTags = null;
			validateNetworks = false;
			
			start = Control.Instance.NearestNode( from, this );
			end = Control.Instance.NearestNode( to, this );
			monitors = new ArrayList();
			solution = null;//new ArrayList();
			seeking = false;
			
			cacheLifespan = Control.NoCache;
			
			Control.Instance.RegisterSeeker( this );
		}
		
		
		
		public Seeker( Vector3 from, Vector3 to, float maxFrameTime, float radius, string[] requiredTags, string[] excludedTags, bool validateNetworks, object data ) : this( from, to, maxFrameTime, radius, data )
		{
			this.requiredTags = requiredTags;
			this.excludedTags = excludedTags;
			this.validateNetworks = validateNetworks;
		}
		
		
		
		public NetworkNodeAsset Start
		{
			get
			{
				return start;
			}
		}
		
		
		
		public NetworkNodeAsset End
		{
			get
			{
				return end;
			}
		}
		
		
		
		public Vector3 From
		{
			get
			{
				return from;
			}
		}
		
		
		
		public Vector3 To
		{
			get
			{
				return to;
			}
		}
		
		
		
		public object Data
		{
			get
			{
				return data;
			}
			set
			{
				data = value;
			}
		}
		
		
		
		public float CacheLifespan
		{
			get
			{
				return cacheLifespan;
			}
			set
			{
				cacheLifespan = value;
			}
		}
		
		
		
		public bool Seeking
		{
			get
			{
				return seeking;
			}
		}
		
		
		
		public bool ValidateNetworks
		{
			get
			{
				return validateNetworks;
			}
			set
			{
				validateNetworks = value;
			}
		}
		
		
		
		public List<ConnectionAsset> Solution
		{
			get
			{
				return solution;
			}
		}
		
		
		
		public bool DoesUse( NetworkAsset network )
		{
			if( end.Network == network )
			{
				return true;
			}
			
			for(int i = 0; i < Solution.Count; i++) {
				if(Solution[i].From.Network == network) return true;
			}
			
			return false;
		}
		
		
		
		public bool DoesUse( NetworkNodeAsset node )
		{
			if( end == node )
			{
				return true;
			}
			
			for(int i = 0; i < Solution.Count; i++) {
				if(Solution[i].From == node) return true;
			}
			
			return false;
		}
		
		
		
		public bool DoesUse( ConnectionAsset connection )
		{
			for(int i = 0; i < Solution.Count; i++) {
				if(Solution[i] == connection) return true;
			}
			
			return false;
		}

		
		
		public void AddMonitor( ISearchMonitor monitor )
		{
			if( !monitors.Contains( monitor ) )
			{
				monitors.Add( monitor );
			}
		}
		
		
		
		public void RemoveMonitor( ISearchMonitor monitor )
		{
			monitors.Remove( monitor );
		}		
		
		
		
		private float GScore( ConnectionAsset connection )
		{
			return connection.Cost;
		}
		
		
		
		private float HScore( ConnectionAsset connection )
		{
			return Vector3.Distance( connection.To.Position, End.Position );
		}
		
		
		
		private Stack<ConnectionAsset> directionReverser = new Stack<ConnectionAsset>();

		private IEnumerator DoSeek()
		{
			Hashtable closedSet = new Hashtable();
			Hashtable openSet = new Hashtable();
//			ArrayList openSetValues = new ArrayList();
			PathData currentPath = null;
			float endTime;
			
			endTime = Time.time + maxFrameTime;
			
			ConnectionAsset[] sc = Start.Connections;
			for( int i = 0; i < sc.Length; i++ )
			{
				ConnectionAsset connection = sc[i];
				if( ValidConnection( connection ) )
				{
					openSet[ connection ] = new PathData( connection, GScore( connection ), HScore( connection ) );
				}
			}
			
			while( seeking && openSet.Count > 0 )
			{
//				openSetValues = new ArrayList( openSet.Values );
//				openSetValues.Sort();
//				currentPath = ( PathData )openSetValues[ 0 ];
				PathData lowestValue = null;
				IEnumerator e = openSet.Values.GetEnumerator();
				e.MoveNext();
				lowestValue = (PathData)e.Current;
				while(e.MoveNext()) {
					PathData p = (PathData)e.Current;
					if(p.CompareTo(lowestValue) < 0)
						lowestValue = p;
				}
				currentPath = lowestValue;
				
				if( currentPath.Destination == End )
				{
					if(solution == null) solution = new List<ConnectionAsset>();
					else solution.Clear();
					
					directionReverser.Clear();
					while(currentPath != null) {
						directionReverser.Push(currentPath.Connection);
						currentPath = currentPath.Next;
					}
					while(directionReverser.Count > 0)
						solution.Add(directionReverser.Pop());
					
					break;
				}
				
				openSet.Remove( currentPath.Connection );
				closedSet[ currentPath.Connection ] = 1;
				
				for(int i = 0; i < currentPath.Connections.Count; i++)
				{
					ConnectionAsset connection = (ConnectionAsset)currentPath.Connections[i];
					if( closedSet.ContainsKey( connection ) || !ValidConnection( connection ) )
					{
						continue;
					}
					
					if( !openSet.Contains( connection ) )
					{
						openSet[ connection ] = new PathData( currentPath, connection, GScore( connection ), HScore( connection ) );
					}
				}
				
				if( Time.time >= endTime )
				{
					yield return 0;
					endTime = Time.time + maxFrameTime;
				}
			}
		}
		
		
		
		public IEnumerator Seek()
		{
			List<ConnectionAsset> cache;
			
			Control.Instance.SeekerStarted( this );
			
			if( seeking )
			// Not nao!
			{
				Debug.LogError( "Seeker is busy" );
			}
			else if( Start == null || End == null )
			// I can't use these two together!
			{
				Debug.LogError( "Start and/or End is null" );
				
				solution = null;//.Clear();
				OnSearchFailed();
			}
			else if( Start == End )
			// No path to be found - we're done!
			{
				solution = null; //.Clear();
				OnSearchCompleted();
			}
			else
			// Seems like we need to do some work...
			{
				cache = Control.Instance.GetCache( this );
				
				if( cache != null )
				// We found a matching cached path!
				{
					// TODO: We probably want a deep copy or something...
					solution = cache;//new ArrayList( cache );
					CacheLifespan = Control.NoCache;
					OnSearchCompleted();
				}
				else
				// No more beating around the bush. Pathfinding is needed!
				{
					solution = null;//.Clear();
				
					seeking = true;
				
					yield return Control.Instance.Owner.StartCoroutine( DoSeek() );
				
					if( solution != null )
					// Wohoo!
					{
						OnSearchCompleted();
					}
					else
					// Aww...
					{
						OnSearchFailed();
					}
				
					seeking = false;
				}
			}
		}
		
		
		
		public void Stop()
		{
			seeking = false;
		}
		
		
		
		public void Kill()
		{
			Control.Instance.SeekerKilled( this );
			monitors.Clear();
		}
		
		
		
		public void Invalidate()
		{
			Stop();
			for( int i = 0; i < monitors.Count; i++ )
			{
				( ( ISearchMonitor )monitors[ i ] ).OnSeekerInvalidated( this );
			}
		}
		
		
		
		private void OnSearchCompleted()
		{
			Control.Instance.OnSearchCompleted( this );
			for( int i = 0; i < monitors.Count; i++ )
			{
				( ( ISearchMonitor )monitors[ i ] ).OnSearchCompleted( this );
			}
		}
		
		
		
		private void OnSearchFailed()
		{
			Control.Instance.OnSearchFailed( this );
			for( int i = 0; i < monitors.Count; i++ )
			{
				( ( ISearchMonitor )monitors[ i ] ).OnSearchFailed( this );
			}
		}
		
		public bool ValidPath( List<ConnectionAsset> path )
		{
			for(int i = 0; i < path.Count; i++) {
				if(!ValidConnection(path[i])) return false;
			}
			
			return true;
		}
		
		
		
		public bool Valid( TaggedAsset asset )
		{
			if(requiredTags != null) {
				foreach( string tag in requiredTags )
				{
					if( !asset.HasTag( tag ) )
					{
						return false;
					}
				}
			}
			if(excludedTags != null) {
				foreach( string tag in excludedTags )
				{
					if( asset.HasTag( tag ) )
					{
						return false;
					}
				}
			}
			return true;
		}



		public bool ValidConnection( ConnectionAsset connection )
		{
			if( !connection.Enabled ||								// Connection is disabled?
				!connection.To.Enabled ||							// Target is disabled?
				connection.Width < radius * 2.0f ||					// Seeker doesn't fit?
				!Valid( connection ) ||								// Tags of connection don't fit our seeker?
				!Valid( connection.To )								// Tags of target don't fit our seeker?
			)
			{
				return false;
			}
			
			return true;
		}
	}
}
