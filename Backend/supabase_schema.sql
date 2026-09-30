-- Run in the Supabase SQL editor.
-- Column names must match PlacementRecord.cs.

create extension if not exists postgis;

create table if not exists public.placements (
  id                 uuid primary key default gen_random_uuid(),
  cloud_anchor_id    text not null,
  image_url          text not null,
  anchor_expires_at  timestamptz,

  lat                double precision not null,
  lon                double precision not null,
  location           geography(Point, 4326)
                     generated always as (ST_SetSRID(ST_MakePoint(lon, lat), 4326)::geography) stored,
  gps_accuracy       real,
  heading            real,

  -- image pose relative to its cloud anchor
  pos_x real, pos_y real, pos_z real,
  rot_x real, rot_y real, rot_z real, rot_w real,
  scale_x real, scale_y real, scale_z real,

  created_at         timestamptz not null default now()
);

create index if not exists placements_location_idx on public.placements using gist (location);

-- Unexpired placements within radius_m meters of the user, closest first
create or replace function public.nearby_placements(user_lat double precision, user_lon double precision, radius_m double precision)
returns setof public.placements
language sql stable
as $$
  select p.*
  from public.placements p
  where ST_DWithin(p.location, ST_SetSRID(ST_MakePoint(user_lon, user_lat), 4326)::geography, radius_m)
    and (p.anchor_expires_at is null or p.anchor_expires_at > now())
  order by p.location <-> ST_SetSRID(ST_MakePoint(user_lon, user_lat), 4326)::geography
  limit 50;
$$;

-- Expose to the API (needed if the project was created with auto-expose of new tables turned off)
grant select, insert on public.placements to anon, authenticated;
grant execute on function public.nearby_placements(double precision, double precision, double precision) to anon, authenticated;

-- Row level security.
-- DEVELOPMENT ONLY: anyone with the anon key can read and insert. Tighten once auth is added.
alter table public.placements enable row level security;

create policy "Anyone can read placements"
  on public.placements for select
  using (true);

create policy "Anyone can insert placements (dev)"
  on public.placements for insert
  with check (true);

-- Public image bucket
insert into storage.buckets (id, name, public)
values ('placement-images', 'placement-images', true)
on conflict (id) do nothing;

create policy "Anyone can upload placement images (dev)"
  on storage.objects for insert
  to anon, authenticated
  with check (bucket_id = 'placement-images');
