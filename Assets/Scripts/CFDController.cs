using UnityEngine;
using System.Collections;

public class CFDController : SingletonMonoBehaviour<CFDController>
{
	private float[] m_density, m_dens_prev;	// density
	private float[] m_vecu, m_vecu_prev;	// current horizontal flow
	private float[] m_vecv, m_vecv_prev;	// current vertical flow
	public float Diffusion;					// diffusion rate
	public float Viscosity;					// viscosity rate
	public int N;							// number of cells on a side
	private int m_size;						// total number of squares...should be (N+2)^2

	public Texture SmokeTex;
	public Color SmokeColor;

	/// <summary>
	/// Initialize size and arrays
	/// </summary>
	void Start ()
	{
		m_size = (N + 2) * (N + 2);

		m_density 	= new float[m_size];
		m_dens_prev = new float[m_size];
		m_vecu 		= new float[m_size];
		m_vecv 		= new float[m_size];
		m_vecu_prev = new float[m_size];
		m_vecv_prev = new float[m_size];
	}

	// void FixedUpdate()
	void Update()
	{
		if (Network.isServer)
			RunTimeStep ();
		// TODO: PERF > Should this be run in FixedUpdate()?  Update()?  Coroutine?  Own thread(s)?  On GPU?
	}

	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
		for (int i = 0; i < m_density.Length; ++i) {
			stream.Serialize(ref m_density[i]);
		}
	}

	/// <summary>
	/// Paint smoke
	/// </summary>
	void OnGUI()
	{
		if (Event.current.type.Equals(EventType.Repaint))
		{
			for (int i = 1; i <= N; i++)
			{
				for (int j = 1; j <= N; j++)
				{
					// Fetch and clamp density
					float density = GetDensityAt(i, j);
					if (density < 1f / 255f)
						continue;
					if (density > 1f)
						density = 1f;

					// Get screen coordinates
					int x = (i - 1) * Screen.width / N;
					int xPlus1 = i* Screen.width / N;
					int y = (j - 1) * Screen.height / N;
					int yPlus1 = j * Screen.height / N;

					Graphics.DrawTexture(new Rect(x, y, xPlus1 - x, yPlus1 - y),
					                     SmokeTex,
					                     new Rect(0, 0, 1, 1),
					                     SmokeTex.width, SmokeTex.width, SmokeTex.height, SmokeTex.height,
					                     new Color(0.5f, 0.5f, 0.5f,
					          					   density / 2f)); // 0.5f is the "neutral value" that "modulates" the color output; I'm not quite sure this is correct
				}
			}
		}
	}

	/// <summary>
	/// Index into the 1D array as if it were a 2D array?
	/// </summary>
	/// <param name="i">row</param>
	/// <param name="j">column</param>
	public int IX(int i, int j)
	{
		return i + (N + 2) * j;
	}

	public void WorldToGrid(Vector3 pos, out int i, out int j)
	{
		// TODO: CLEANUP > perhaps the grid should be relative to world space, not screen space?

		// TODO: BUG > I think this is off by some factor of the width of the sprite or something

		Vector3 screen_pos = Camera.main.WorldToScreenPoint(pos);
		i = ((int)screen_pos.x * N / Screen.width) % N;
		j = ((Screen.height - (int)screen_pos.y) * N / Screen.height) % N;
	}

	/// <summary>
	/// Advect?
	/// </summary>
	private void Advect(int b, float[] d, float[] d0, float[] u, float[] v)
	{
		int i0, j0, i1, j1;
		float x, y, s0, t0, s1, t1, dt0;
		
		dt0 = Time.deltaTime * N;
		for (int i = 1; i <= N; i++)
		{
			for (int j = 1; j <= N; j++)
			{
				x = i - dt0 * u[IX(i,j)];
				y = j - dt0 * v[IX(i,j)];

				// perform a clamp
				if (x < 0.5f) x = 0.5f; 
				if (x > N + 0.5f) x = N + 0.5f;
				i0 = (int)x; 
				i1 = i0 + 1;
				if (y < 0.5f) y = 0.5f;
				if (y > N + 0.5f) y = N + 0.5f; 
				j0 = (int)y; 
				j1 = j0 + 1;
				s1 = x-i0; s0 = 1 - s1; t1 = y - j0; t0 = 1 - t1;
				d[IX(i,j)] = s0 * (t0 * d0[IX(i0,j0)] + t1 * d0[IX(i0,j1)]) + s1 * (t0 * d0[IX(i1,j0)] + t1 * d0[IX(i1,j1)]);
			}
		}
		SetBound (b, d);
	}

	/// <summary>
	/// Run a single density timestep
	/// </summary>
	public void DensityStep(float[] x, float[] x0, float[] u, float[] v)
	{
		Diffuse (0, x0, x, Diffusion);
		Advect (0, x, x0, u, v);
	}

	/// <summary>
	/// Diffuse?
	/// </summary>
	private void Diffuse(int b, float[] x, float[] x0, float diff)
	{
		float a = Time.deltaTime * diff * N * N;
		LinearSolve (b, x, x0, a, 1 + 4*a);
	}

	/// <summary>
	/// ?
	/// </summary>
	private void LinearSolve(int b, float[] x, float[] x0, float a, float c )
	{
		for (int k = 0; k < 20; k++)
		{
			for (int i=1; i <= N ; i++)
			{
				for (int j=1; j<=N; j++)
				{
					x[IX(i,j)] = (x0[IX(i,j)] + a*(x[IX(i-1,j)]+x[IX(i+1,j)]+x[IX(i,j-1)]+x[IX(i,j+1)]))/c;
				}
			}
			SetBound (b, x);
		}
	}

	/// <summary>
	/// Project?
	/// </summary>
	private void Project(float[] u, float[] v, float[] p, float[] div )
	{
		for (int i=1; i<=N; i++)
		{
			for (int j=1; j<=N; j++)
			{
				div[IX(i,j)] = -0.5f * (u[IX(i+1,j)] - u[IX(i-1,j)] + v[IX(i,j+1)] - v[IX(i,j-1)]) / N;
				p[IX(i,j)] = 0f;
			}
		}
		SetBound (0, div);
		SetBound (0, p);
		
		LinearSolve (0, p, div, 1, 4);
		
		for (int i=1; i<=N; i++)
		{
			for (int j=1; j<=N; j++)
			{
				u[IX(i,j)] -= 0.5f * N * (p[IX(i+1,j)] - p[IX(i-1,j)]);
				v[IX(i,j)] -= 0.5f * N * (p[IX(i,j+1)] - p[IX(i,j-1)]);
			}
		}
		SetBound (1, u);
		SetBound (2, v);
	}

	/// <summary>
	/// Set boundary condition?
	/// </summary>
	public void SetBound(int b, float[] x)
	{
		for (int i = 1; i <= N; i++) 
		{
			x[IX(0,   i  )] = b == 1 ? -x[IX(1,i )] : x[IX(1, i)];
			x[IX(N+1, i  )] = b == 1 ? -x[IX(N, i)] : x[IX(N, i)];
			x[IX(i  , 0  )] = b == 2 ? -x[IX(i, 1)] : x[IX(i, 1)];
			x[IX(i  , N+1)] = b == 2 ? -x[IX(i, N)] : x[IX(i, N)];
		}
		x[IX(0  , 0  )] = 0.5f * (x[IX(1, 0  )] + x[IX(0  , 1 )]);
		x[IX(0  , N+1)] = 0.5f * (x[IX(1, N+1)] + x[IX(0  , N)]);
		x[IX(N+1, 0  )] = 0.5f * (x[IX(N, 0  )] + x[IX(N+1, 1 )]);
		x[IX(N+1, N+1)] = 0.5f * (x[IX(N, N+1)] + x[IX(N+1, N)]);
	}

	/// <summary>
	/// Run a single velocity timestep
	/// </summary>
	public void VelocityStep(float[] u, float[] v, float[] u0, float[] v0)
	{
		Diffuse (1, u0, u, Viscosity);
		Diffuse (2, v0, v, Viscosity);
		
		Project (u0, v0, u, v );
		
		Advect (1, u, u0, u0, v0); 
		Advect (2, v, v0, u0, v0);

		Project (u, v, u0, v0 );
	}

	/// <summary>
	/// Get the smoke density at a given position
	/// </summary>
	/// <returns>Smoke density</returns>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	public float GetDensityAt(int x, int y)
	{
		return m_density[IX(x,y)];
	}

	/// <summary>
	/// Get horizontal flow
	/// </summary>
	/// <returns>Vertical flow at position</returns>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	public float GetUAt(int x, int y)
	{
		return m_vecu[IX(x,y)];
	}

	/// <summary>
	/// Get vertical flow
	/// </summary>
	/// <returns>Vertical flow at position</returns>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	public float GetVAt(int x, int y)
	{
		return m_vecv[IX(x,y)];
	}

	/// <summary>
	/// Add smoke to a specific cell
	/// </summary>
	/// <param name="source">Amount of smoke to add</param>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	[RPC]
	public void AddDensityAt(float source, int x, int y)
	{
		if (Network.isServer)
			m_density[IX(x,y)] += source;
	}

	/// <summary>
	/// Run a single simulation timestep
	/// </summary>
	/// <param name="dt">Timestep, in seconds</param>
	private void RunTimeStep()
	{
		VelocityStep(m_vecu, m_vecv, m_vecu_prev ,m_vecv_prev);
		DensityStep(m_density, m_dens_prev, m_vecu, m_vecv);
		RemoveFromAll();
	}

	/// <summary>
	/// Add horizontal force
	/// </summary>
	/// <param name="force">Force</param>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	public void AddUForce(float force, int x, int y)
	{
		m_vecu[IX(x,y)] += force;
	}

	/// <summary>
	/// Add vertical force
	/// </summary>
	/// <param name="force">Force</param>
	/// <param name="x">x position</param>
	/// <param name="y">y position</param>
	public void AddVForce(float force, int x, int y)
	{
		m_vecv[IX(x,y)] += force;
	}

	/// <summary>
	/// Clear all smoke
	/// </summary>
	public void ClearAll()
	{
		for (int i = 0; i < m_size; i++)
			m_density[i] = m_dens_prev[i] = m_vecu[i] = m_vecv[i] = m_vecu_prev[i] = m_vecv_prev[i] = 0f;
	}

	/// <summary>
	/// Drain smoke from all cells
	/// </summary>
	/// <param name="dt">Timestep in seconds</param>
	private void RemoveFromAll()
	{
		for (int i = 1; i <= N; i++ )
		{
			for (int j = 1; j <= N; j++ )
			{
				m_density[IX(i,j)] = m_density[IX(i,j)] * (1f - (Time.deltaTime / 50f));
			}
		}
	}

	//Returns if an array index is valid
	public bool IsInRange(int a) {
		return a >= 0 && a <= N;
	}
}
