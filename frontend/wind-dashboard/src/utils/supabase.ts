import { createClient } from '@supabase/supabase-js';

const supabaseUrl = "https://rtghegkdrfacecscgysv.supabase.co"
const supabaseKey = "sb_publishable_VKRs61rlBlNc7Y5CXXugPA_L1CCwxSh"

export const supabase = createClient(supabaseUrl, supabaseKey);