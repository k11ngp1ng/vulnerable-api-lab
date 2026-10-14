-- Fictional local data for the learning lab. No plaintext passwords or real identities.
CREATE TABLE IF NOT EXISTS lab_users (
    id text PRIMARY KEY,
    display_name text NOT NULL,
    login_name text UNIQUE,
    password_hash text
);

-- Existing local volumes were created before the Day 3 credential columns.
ALTER TABLE lab_users ADD COLUMN IF NOT EXISTS login_name text;
ALTER TABLE lab_users ADD COLUMN IF NOT EXISTS password_hash text;
CREATE UNIQUE INDEX IF NOT EXISTS lab_users_login_name_key ON lab_users (login_name);

CREATE TABLE IF NOT EXISTS notes (
    id integer PRIMARY KEY,
    owner_id text NOT NULL REFERENCES lab_users(id),
    title text NOT NULL,
    content text NOT NULL
);

INSERT INTO lab_users (id, display_name) VALUES
    ('user-a', 'Fictional User A'),
    ('user-b', 'Fictional User B')
ON CONFLICT (id) DO NOTHING;

INSERT INTO notes (id, owner_id, title, content) VALUES
    (1, 'user-a', 'Lab note A', 'Fictional content for local tests.'),
    (2, 'user-b', 'Lab note B', 'More fictional content for local tests.')
ON CONFLICT (id) DO NOTHING;
