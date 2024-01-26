CREATE TABLE public.looseitems
(
    id            UUID      NOT NULL DEFAULT gen_random_uuid(),
    sequential_id INT8      NOT NULL GENERATED ALWAYS AS IDENTITY,
    created       TIMESTAMP NOT NULL,
    description   STRING(100) NOT NULL,
    CONSTRAINT looseitems_pkey PRIMARY KEY (id ASC),
    UNIQUE INDEX looseitems_sequential_id_index (sequential_id ASC),
    INDEX looseitems_created_index (created ASC),
    INDEX "looseitems_ordering_index" (created, sequential_id) STORING (description)
)