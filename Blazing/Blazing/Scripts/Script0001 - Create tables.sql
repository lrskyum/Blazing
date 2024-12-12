create table export_import_experiment
(
    id             bigint not null default nextval('export_import_experiment_id_seq'),
    version        bigint,
    timestamp      text,
    ssoid          text,
    account_number text,
    product_name   text
);
