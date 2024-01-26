create table public."packs"
(
    id            UUID primary key default gen_random_uuid(),
    sequential_id INT8      not null generated always as identity,
    created       TIMESTAMP not null,
    name          string(50) not null,
    unique index "packs_sequential_id_index" (sequential_id asc),
    index "packs_created_index" (created asc)
);

drop table public."pack_contents";

create table public."pack_contents"
(
    id            UUID primary key default gen_random_uuid(),
    sequential_id INT8 not null generated always as identity,
    pack_id       UUID not null,
    description   STRING(100) not null,
    unique index "pack_contents_id_index" (id),
    unique index "pack_contents_index" (pack_id, id),
    constraint "pack_contents_pack_ref" foreign key (pack_id) references "packs" (id),
    family "pack_contents_items" (pack_id, sequential_id, id)
);

create view "pack_contents_view" as
select "pack_contents".id            "id",
       "packs".created               "created",
       "pack_contents".sequential_id "sequential_id",
       "pack_contents".description   "description",
       "packs".id                    "pack_id",
       "packs".name                  "pack_name"
from "packs"
         join "pack_contents" on ("packs".id = "pack_contents".pack_id);
