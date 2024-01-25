create table public."packs" (
    id UUID primary key default gen_random_uuid(),
    created TIMESTAMP not null,
    sequential_id INT8 not null generated always as identity,
    name string(50) not null,
    unique index "packs_sequential_id_index" (sequential_id asc),
    index "packs_created_index" (created asc) 
)