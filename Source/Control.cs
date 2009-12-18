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
	public class Control
	{
		private class CachedSeeker
		{
			private NetworkNodeAsset start, end;
			private List<ConnectionAsset> solution;
			
			private float lifespan, endTime;
			
			
			
			public CachedSeeker( Seeker seeker )
			{
				this.start = seeker.Start;
				this.end = seeker.End;
				// TODO: Do we NEED to copy this?
				this.solution = new List<ConnectionAsset>( seeker.Solution );
				
				Restart( seeker.CacheLifespan );
			}
			
			
			
			public bool Match( Seeker seeker )
			{
				return this.start == seeker.Start && this.end == seeker.End && seeker.ValidPath( Solution );
			}
			
			
			
			public List<ConnectionAsset> Solution
			{
				get
				{
					return solution;
				}
			}
			
			
			
			public void Restart( float lifespan )
			{
				this.lifespan = lifespan;
				endTime = Time.time + lifespan;
			}
			
			
			
			public bool Valid
			{
				get
				{
					return lifespan < Control.NoCache || Time.time < endTime;
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
				
/*				PathData current = Solution;
				while(current != null) {
					if(current.Connection.From.Network == network) return true;
					current = current.Next;
				} */
				
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
				
				
/*				PathData current = Solution;
				while(current != null) {
					if(current.Connection.From == node) return true;
					current = current.Next;
				} */
				
				return false;
			}
			
			
			
			public bool DoesUse( ConnectionAsset connection )
			{
				for(int i = 0; i < Solution.Count; i++) {
					if(Solution[i] == connection) return true;
				}

/*				PathData current = Solution;
				while(current != null) {
					if(current.Connection == connection) return true;
					current = current.Next;
				} */
				
				return false;
			}
		}
		
		
		private static Control instance;
		
		
		
		public static Control Instance
		{
			get
			{
				if( instance == null )
				{
					new Control();
				}
				
				return instance;
			}
		}
		
		
		public const float NoCache = 0.0f;
		public const float IndefinitelyCache = -1.0f;
		
		private ArrayList networks, idleSeekers, activeSeekers, usedSeekers, cachedSeekers;
		private float seekerCacheLifespan;
		private bool autoRecalculate;
		private MonoBehaviour monoBehaviour;
		
		
		
		public Control()
		{
			instance = this;
			networks = new ArrayList();
			idleSeekers = new ArrayList();
			activeSeekers = new ArrayList();
			usedSeekers = new ArrayList();
			cachedSeekers = new ArrayList();
			seekerCacheLifespan = NoCache;
			autoRecalculate = true;
		}
		
		
		
		public void Init( MonoBehaviour monoBehaviour )
		{
			this.monoBehaviour = monoBehaviour;
			Owner.StartCoroutine( UpdateCache() );
			Debug.Log( "AngryAnt " + Resources.Version + " - runtime online. Ready to load collections." );
		}
		
		
		
		public MonoBehaviour Owner
		{
			get
			{
				return monoBehaviour;
			}
		}
		
		
		
		public float SeekerCacheLifespan
		{
			get
			{
				return seekerCacheLifespan;
			}
			set
			{
				seekerCacheLifespan = value;
			}
		}
		
		
		
		public bool AutoRecalculate
		{
			get
			{
				return autoRecalculate;
			}
			set
			{
				autoRecalculate = value;
			}
		}
		
		
		
		private IEnumerator UpdateCache()
		{
			CachedSeeker cachedSeeker;
			
			while( true )
			{
				yield return new WaitForEndOfFrame();
				
				for( int i = 0; i < cachedSeekers.Count; )
				{
					cachedSeeker = ( CachedSeeker )cachedSeekers[ i ];
					if( !cachedSeeker.Valid )
					{
						cachedSeekers.Remove( cachedSeeker );
					}
					else
					{
						i++;
					}
				}
			}
		}
		
		
		
		public List<ConnectionAsset> GetCache( Seeker seeker )
		{
			// TODO: Make this an O(1) lookup?
			foreach( CachedSeeker cache in cachedSeekers )
			{
				if( cache.Match( seeker ) )
				{
					if( seeker.CacheLifespan != NoCache )
					{
						cache.Restart( seeker.CacheLifespan );
					}
					
					return cache.Solution;
				}
			}
			
			return null;
		}
		
		
		
		public void RegisterSeeker( Seeker seeker )
		{
			idleSeekers.Add( seeker );
			seeker.CacheLifespan = seekerCacheLifespan;
		}
		
		
		
		public void SeekerStarted( Seeker seeker )
		{
			idleSeekers.Remove( seeker );
			if( !activeSeekers.Contains( seeker ) )
			{
				activeSeekers.Add( seeker );
			}
		}
		
		
		
		public void SeekerKilled( Seeker seeker )
		{
			idleSeekers.Remove( seeker );
			activeSeekers.Remove( seeker );
			usedSeekers.Remove( seeker );
		}
		
		
		
		public void UseSeeker( Seeker seeker )
		{
			idleSeekers.Remove( seeker );
			activeSeekers.Remove( seeker );
			if( !usedSeekers.Contains( seeker ) )
			{
				usedSeekers.Add( seeker );
			}
		}
		
		
		
		public void UncacheSeeker( Seeker seeker )
		{
			cachedSeekers.Remove( seeker );
		}
		
		
		
		public bool LoadCollection( IPathAsset asset )
		{
			CollectionAsset collection;
			
			if( asset == null )
			{
				return false;
			}
			
			collection = CollectionAsset.LoadFromData( asset.Data, asset );
			
			foreach( NetworkAsset network in collection.Networks )
			{
				networks.Add( network );
			}
			
			Debug.Log( "AngryAnt " + Resources.Version + " - loaded collection. Network count now up to " + networks.Count + "." );
			
			return true;
		}
		
		
		
		public int IdleSeekers
		{
			get
			{
				return idleSeekers.Count;
			}
		}
		
		
		
		public int ActiveSeekers
		{
			get
			{
				return activeSeekers.Count;
			}
		}
		
		
		
		public int UsedSeekers
		{
			get
			{
				return usedSeekers.Count;
			}
		}
		
		
		
		public int CachedSeekers
		{
			get
			{
				return cachedSeekers.Count;
			}
		}
		
		
		
		// TODO: Is caching this safe?
		private ArrayList gridNetworks = null;
		public ArrayList GridNetworks
		{
			get
			{
				if(gridNetworks == null) {
					gridNetworks = new ArrayList();
				
					for( int i = 0; i < networks.Count; i++ )
					{
						if( networks[i] is GridNetworkAsset )
						{
							gridNetworks.Add( networks[i] );
						}
					}
				}
				return gridNetworks;
			}
		}
		
		
		
		public NetworkAsset GetNetwork( string name )
		{
			for( int i = 0; i < networks.Count; i++ )
			{
				if( ((NetworkAsset)networks[i]).Name == name )
				{
					return (NetworkAsset)networks[i];
				}
			}
			
			return null;
		}
		
		
		
		private ArrayList gridNodes = null;
		public ArrayList GetGridNodes( NetworkNodeAsset node )
		{
			// TODO: Is caching this safe, or should we be clearing and starting over?
			if(gridNodes == null) {
				gridNodes = new ArrayList();
				ArrayList gridNets = GridNetworks;
				for(int i = 0; i < gridNets.Count; i++)
				{
					GridNetworkAsset network = (GridNetworkAsset)gridNets[i];
					for(int j = 0; j < network.Nodes.Length; j++)
					{
						GridNodeAsset gridNode = (GridNodeAsset)network.Nodes[j];
						if(gridNode.Target == node) gridNodes.Add(gridNode);
					}
				}
			}
			return gridNodes;
		}
		
		
		
		public ArrayList GetNetworks( string name )
		{
			ArrayList found;
			
			found = new ArrayList();
			
			foreach( NetworkAsset network in networks )
			{
				if( network.Name == name )
				{
					found.Add( network );
				}
			}
			
			return found;
		}
		
		
		
		public void RecalculateNetwork( NetworkAsset network )
		{
			CachedSeeker cache;
			Seeker seeker;

			// TODO: Implement for active seekers as well (we need to get out of the seeker coroutine without doing OnCompleted/OnFailed)
			
			for( int i = 0; i < cachedSeekers.Count; )
			{
				cache = ( CachedSeeker )cachedSeekers[ i ];
				
				if( cache.DoesUse( network ) )
				{
					cachedSeekers.Remove( cache );
				}
				else
				{
					i++;
				}
			}
			
			for( int i = 0; i < usedSeekers.Count; )
			{
				seeker = ( Seeker )usedSeekers[ i ];
				
				if( seeker.DoesUse( network ) )
				{
					usedSeekers.Remove( seeker );
					seeker.Invalidate();
				}
				else
				{
					i++;
				}
			}
		}
		
		
		
		public void RecalculateNetworks( ArrayList networks )
		{
			foreach( NetworkAsset network in networks )
			{
				RecalculateNetwork( network );
			}
		}
		
		
		
		public void RecalculateNode( NetworkNodeAsset node )
		{
			CachedSeeker cache;
			Seeker seeker;

			// TODO: Implement for active seekers as well (we need to get out of the seeker coroutine without doing OnCompleted/OnFailed)
			
			for( int i = 0; i < cachedSeekers.Count; )
			{
				cache = ( CachedSeeker )cachedSeekers[ i ];
				
				if( cache.DoesUse( node ) )
				{
					cachedSeekers.Remove( cache );
				}
				else
				{
					i++;
				}
			}
			
			for( int i = 0; i < usedSeekers.Count; )
			{
				seeker = ( Seeker )usedSeekers[ i ];
				
				if( seeker.DoesUse( node ) )
				{
					usedSeekers.Remove( seeker );
					seeker.Invalidate();
				}
				else
				{
					i++;
				}
			}
		}
		
		
		
		public void RecalculateNodes( ArrayList nodes )
		{
			foreach( NetworkNodeAsset node in nodes )
			{
				RecalculateNode( node );
			}
		}
		
		
		
		public void RecalculateConnection( ConnectionAsset connection )
		{
			CachedSeeker cache;
			Seeker seeker;

			// TODO: Implement for active seekers as well (we need to get out of the seeker coroutine without doing OnCompleted/OnFailed)
			
			for( int i = 0; i < cachedSeekers.Count; )
			{
				cache = ( CachedSeeker )cachedSeekers[ i ];
				
				if( cache.DoesUse( connection ) )
				{
					cachedSeekers.Remove( cache );
				}
				else
				{
					i++;
				}
			}
			
			for( int i = 0; i < usedSeekers.Count; )
			{
				seeker = ( Seeker )usedSeekers[ i ];
				
				if( seeker.DoesUse( connection ) )
				{
					usedSeekers.Remove( seeker );
					seeker.Invalidate();
				}
				else
				{
					i++;
				}
			}
		}
		
		
		
		public void RecalculateConnections( ArrayList connections )
		{
			foreach( ConnectionAsset connection in connections )
			{
				RecalculateConnection( connection );
			}
		}
		
		
		
		public void OnDisabled( NetworkAsset network )
		{
			if( autoRecalculate )
			{
				RecalculateNetwork( network );
			}
		}
		
		
		
		public void OnDisabled( NetworkNodeAsset node )
		{
			if( autoRecalculate )
			{
				RecalculateNode( node );
			}
		}
		
		
		
		public void OnDisabled( ConnectionAsset connection )
		{
			if( autoRecalculate )
			{
				RecalculateConnection( connection );
			}
		}
		
		
//		private static int count = 0;
		private Dictionary <NetworkAsset,Bounds> networkBoundsCache = new Dictionary <NetworkAsset,Bounds>();
		public NetworkNodeAsset NearestNode( Vector3 position, Seeker seeker )
		{
//			count++;
//			Debug.Log("NearestNode: " + count);
			NetworkNodeAsset nearest;
//			Bounds networkBounds;
			
			nearest = null;
			for( int i = 0; i < networks.Count; i++ )
			{
				NetworkAsset network = (NetworkAsset)networks[i];
				if( network is GridNetworkAsset || !network.Enabled || ( seeker.ValidateNetworks && !seeker.Valid( network ) ) )
				// Reject disabled networks and invalid ones if we're checking for that
				{
					continue;
				}
				
				if(!networkBoundsCache.ContainsKey(network))
					networkBoundsCache[network] = new Bounds( network.Position + Owner.transform.position, network.Size );
				if( !networkBoundsCache[network].Contains( position ) )
				// Reject networks whose bounds do not contain the point
				{
					continue;
				}
				
				foreach( NetworkNodeAsset node in network.Nodes )
				// Find the nearest node in this network
				{
					if( !node.Enabled || !seeker.Valid( node ) )
					{
						continue;
					}
					
					if( nearest == null || ( WorldPosition( nearest ) - position ).sqrMagnitude > ( WorldPosition( node ) - position ).sqrMagnitude )
					{
						nearest = node;
					}
				}
			}
			
			return nearest;
		}
		
		
		
		public Vector3 WorldPosition( NetworkNodeAsset node )
		{
			return node.Position + node.Network.Position + Owner.transform.position;
		}
		
		
		
		public bool StartSeeker( Seeker seeker )
		{
			if( monoBehaviour == null )
			{
				Debug.LogError( "Control failed to add seeker: Not initialized." );
				return false;
			}
			
			monoBehaviour.StartCoroutine( seeker.Seek() );
			
			return true;
		}
		
		
		
		public void OnSearchCompleted( Seeker seeker )
		{
			UseSeeker( seeker );
			if( seeker.CacheLifespan != NoCache )
			{
				cachedSeekers.Add( new CachedSeeker( seeker ) );
			}
		}
		
		
		
		public void OnSearchFailed( Seeker seeker )
		{
			idleSeekers.Add( seeker );
			activeSeekers.Remove( seeker );
		}
		
		
/*		
		public static float PathCost( IList path, Vector3 to )
		{
			float cost;
			
			if( path.Count == 0 )
			{
				return 0.0f;
			}
			
			cost = 0.0f;
			
			foreach( ConnectionAsset connection in path )
			{
				cost += connection.Cost;
			}
			
			return cost + ( to - ( ( ConnectionAsset )path[ path.Count - 1 ] ).To.Position ).magnitude;
		}
		
		
		
		public static float PathCost( Vector3 from, IList path, Vector3 to )
		{
			if( path.Count == 0 )
			{
				return ( from - to ).magnitude;
			}
			
			return PathCost( path, to ) + ( from - ( ( ConnectionAsset )path[ 0 ] ).From.Position ).magnitude;
		}
		
		
		
		public static IList SortConnections( IList connections, Vector3 destination )
		{
			ArrayList less, greater;
			ConnectionAsset pivot;
			float pivotCost;
			ConnectionAsset[] result;
			
			if( connections.Count <= 1 )
			{
				return connections;
			}
			
			less = new ArrayList();
			greater = new ArrayList();
			
			// TODO: Figure out why using AbsolutePosition messes this up badly?
			
			// Pick a pivot //
			pivot = ( ConnectionAsset )connections[ connections.Count / 2 ];
			pivotCost = ( destination - pivot.To.Position/*AbsolutePosition( Control.Instance.Owner.transform )* / ).magnitude + pivot.Cost;
			
			// Split in less and greater lists //
			foreach( ConnectionAsset connection in connections )
			{
				if( connection == pivot )
				{
					continue;
				}
				
				if( ( destination - connection.To.Position/*AbsolutePosition( Control.Instance.Owner.transform ) * /).magnitude + connection.Cost < pivotCost )
				{
					less.Add( connection );
				}
				else
				{
					greater.Add( connection );
				}
			}
			
			// Sort less and greater //
			less = ( less.Count > 0 ) ? new ArrayList( SortConnections( less, destination ) ) : less;			
			greater = ( greater.Count > 0 ) ? new ArrayList( SortConnections( greater, destination ) ) : greater;
			
			// Create the result from less + pivot + greater //
			result = new ConnectionAsset[ connections.Count ];
			System.Array.Copy( less.ToArray( typeof( ConnectionAsset ) ), 0, result, 0, less.Count );
			result[ less.Count ] = pivot;
			System.Array.Copy( greater.ToArray( typeof( ConnectionAsset ) ), 0, result, less.Count + 1, greater.Count );
			
			return result;
		}
*/	}
}
